using LinqExpressions = System.Linq.Expressions;

namespace Expressions.Utilities;

/// <summary>
/// Compiles an expression tree to intermediate language, while properly dealing with types,
/// methods, properties and what not that haven't been actually instantiated yet.
/// </summary>
public class ExpressionCompiler : ExpressionVisitor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionCompiler"/> class.
    /// </summary>
    /// <param name="ilGenerator">The IL generator to compile to.</param>
    /// <param name="parameters">The parameters, if any.</param>
    public ExpressionCompiler(ILGenerator ilGenerator, params LinqExpressions.ParameterExpression[] parameters)
    {
        // get all parameters and their offset
        _parameters = new Dictionary<LinqExpressions.ParameterExpression, int>();
        for (int i = 0; i < parameters.Length; i++)
            _parameters[parameters[i]] = i + 1;

        _ilGenerator = ilGenerator;
    }

    private readonly ILGenerator _ilGenerator;
    private readonly Dictionary<LinqExpressions.ParameterExpression, int> _parameters;
    private readonly Dictionary<LinqExpressions.ParameterExpression, LocalBuilder> _locals = new();
    private bool _byReference = false;

    private void Visit(LinqExpressions.Expression node, bool wantVoid)
    {
        Visit(node);

        // did we actually want a void expression?
        if (wantVoid && node.Type != typeof(void))
            _ilGenerator.Emit(OpCodes.Pop);
    }

    /// <inheritdoc/>
    public override LinqExpressions.Expression Visit(LinqExpressions.Expression node)
    {
        var oldByReference = _byReference;
        _byReference = false;
        var result = base.Visit(node);
        _byReference = oldByReference;

        return result;
    }

    /// <inheritdoc/>
    public LinqExpressions.Expression VisitByReference(LinqExpressions.Expression node)
    {
        var oldByReference = _byReference;
        _byReference = node.Type.IsValueType;
        var result = base.Visit(node);

        // handle reference conversion if we hadn't done so yet
        if (_byReference)
        {
            var local = _ilGenerator.DeclareLocal(node.Type);
            _ilGenerator.Emit(OpCodes.Stloc, local);
            _ilGenerator.Emit(OpCodes.Ldloca, local);
        }

        _byReference = oldByReference;

        return result;
    }

    /// <inheritdoc/>
    protected override void VisitUnhandled(object node)
    {
        throw new NotSupportedException(string.Format("{0} is not supported", node.GetType().Name));
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitBinary(LinqExpressions.BinaryExpression node)
    {
        // assignment goes in the other direction, so it's handled specially here
        if (node.NodeType == LinqExpressions.ExpressionType.Assign)
        {
            // visit the source
            Visit(node.Right);

            // duplicate the value, because this expression evaluates to it as well
            _ilGenerator.Emit(OpCodes.Dup);

            // determine how to assign the result
            if (node.Left is LinqExpressions.ParameterExpression parameterExpression)
            {
                // store into parameter or local
                if (_parameters.TryGetValue(parameterExpression, out int index))
                    _ilGenerator.Emit(OpCodes.Starg, (short)index);
                else if (_locals.TryGetValue(parameterExpression, out LocalBuilder local))
                    _ilGenerator.Emit(OpCodes.Stloc, local);
                else
                    throw new NotSupportedException();
            }
            else if (node.Left is LinqExpressions.MemberExpression memberExpression)
            {
                // do we have an instance?
                if (memberExpression.Expression != null)
                {
                    // place the value-to-be in a temporary, because it has to come _first_ on
                    // the stack (you can't make this stuff up)
                    var temporaryLocal = _ilGenerator.DeclareLocal(node.Right.Type);
                    _ilGenerator.Emit(OpCodes.Stloc, temporaryLocal);

                    // emit the target instance
                    VisitByReference(memberExpression.Expression);

                    // emit code to store into the field
                    _ilGenerator.Emit(OpCodes.Ldloc, temporaryLocal);
                    _ilGenerator.Emit(OpCodes.Stfld, (FieldInfo)memberExpression.Member);
                }
                else
                {
                    // emit code to store into the static field
                    _ilGenerator.Emit(OpCodes.Stsfld, (FieldInfo)memberExpression.Member);
                }
            }

            return node;
        }

        // emit code to perform the operation
        switch (node.NodeType)
        {
            case LinqExpressions.ExpressionType.Equal:
                // emit the operands
                Visit(node.Left);
                Visit(node.Right);

                // emit the test
                _ilGenerator.Emit(OpCodes.Ceq);
                break;

            case LinqExpressions.ExpressionType.NotEqual:
                // emit the operands
                Visit(node.Left);
                Visit(node.Right);

                // emit the test
                _ilGenerator.Emit(OpCodes.Ceq);
                _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                _ilGenerator.Emit(OpCodes.Xor);
                break;

            case LinqExpressions.ExpressionType.AndAlso:
                {
                    // emit the left-hand side first
                    var end = _ilGenerator.DefineLabel();
                    Visit(node.Left);

                    // if it wasn't true, break away
                    _ilGenerator.Emit(OpCodes.Dup);
                    _ilGenerator.Emit(OpCodes.Brfalse, end);
                    _ilGenerator.Emit(OpCodes.Pop);

                    // emit the right-hand side
                    Visit(node.Right);

                    // we end up here, whatever happens
                    _ilGenerator.MarkLabel(end);
                }
                break;

            default:
                throw new NotImplementedException();
        }

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitBlock(LinqExpressions.BlockExpression node)
    {
        // add all local variables
        foreach (var local in node.Variables)
            _locals.Add(local, _ilGenerator.DeclareLocal(local.Type));

        // emit all expressions
        foreach (var expression in node.Expressions)
            Visit(expression, wantVoid: expression != node.Result);

        // remove any result if returning void
        if (node.Type == typeof(void) && node.Result.Type != typeof(void))
            _ilGenerator.Emit(OpCodes.Pop);

        // remove all local variables
        foreach (var local in node.Variables)
            _locals.Remove(local);

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitConstant(LinqExpressions.ConstantExpression node)
    {
        if (node.Type == typeof(bool))
            _ilGenerator.Emit((bool)node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        else if (node.Type == typeof(int))
            _ilGenerator.Emit(OpCodes.Ldc_I4, (int)node.Value);
        else if (!node.Type.IsValueType && node.Value == null)
            _ilGenerator.Emit(OpCodes.Ldnull);
        else if (node.Type == typeof(string))
            _ilGenerator.Emit(OpCodes.Ldstr, (string)node.Value);
        else if (typeof(Type).IsAssignableFrom(node.Type))
        {
            _ilGenerator.Emit(OpCodes.Ldtoken, (Type)node.Value);
            _ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static));
        }
        else if (node.Type.IsNullable() && node.Value == null)
        {
            var temporaryLocal = _ilGenerator.DeclareLocal(node.Type);
            _ilGenerator.Emit(OpCodes.Ldloca, temporaryLocal);
            _ilGenerator.Emit(OpCodes.Initobj, node.Type);
            _ilGenerator.Emit(OpCodes.Ldloc, temporaryLocal);
        }
        else
            throw new NotImplementedException();

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitConditional(LinqExpressions.ConditionalExpression node)
    {
        // emit the test result
        Visit(node.Test);

        // generate labels
        var ifFalse = _ilGenerator.DefineLabel();
        var endIf = _ilGenerator.DefineLabel();

        // branch to the appropriate label if the test was false
        _ilGenerator.Emit(OpCodes.Brfalse, ifFalse);

        // emit the true case
        Visit(node.IfTrue, wantVoid: node.Type == typeof(void));
        _ilGenerator.Emit(OpCodes.Br, endIf);

        // emit the false case
        _ilGenerator.MarkLabel(ifFalse);
        Visit(node.IfFalse, wantVoid: node.Type == typeof(void));

        // and we're done
        _ilGenerator.MarkLabel(endIf);

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitDefault(LinqExpressions.DefaultExpression node)
    {
        if (node.Type != typeof(void))
            throw new NotImplementedException();

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitMember(LinqExpressions.MemberExpression node)
    {
        // do we have an instance to get the property from?
        if (node.Expression != null)
        {
            // emit the source
            VisitByReference(node.Expression);

            // emit the field access
            _ilGenerator.Emit(_byReference ? OpCodes.Ldflda : OpCodes.Ldfld, (FieldInfo)node.Member);
        }
        else
        {
            // emit the field access
            _ilGenerator.Emit(_byReference ? OpCodes.Ldsflda : OpCodes.Ldsfld, (FieldInfo)node.Member);
        }

        // we've handled reference conversion
        _byReference = false;

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitMethodCall(LinqExpressions.MethodCallExpression node)
    {
        // emit the object and arguments
        if (node.Object != null)
            VisitByReference(node.Object);
        foreach (var argument in node.Arguments)
            Visit(argument);

        // call the method
        _ilGenerator.Emit(node.Method.IsVirtual || node.Method.DeclaringType.IsInterface ? OpCodes.Callvirt : OpCodes.Call, node.Method);

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitNew(LinqExpressions.NewExpression node)
    {
        // emit all arguments
        foreach (var argument in node.Arguments)
            Visit(argument);

        // emit the constructor call
        _ilGenerator.Emit(OpCodes.Newobj, node.Constructor);

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitParameter(LinqExpressions.ParameterExpression node)
    {
        // emit code to get the parameter or local
        if (_parameters.TryGetValue(node, out int index))
            _ilGenerator.Emit(_byReference ? OpCodes.Ldarga : OpCodes.Ldarg, (short)index);
        else if (_locals.TryGetValue(node, out LocalBuilder local))
            _ilGenerator.Emit(_byReference ? OpCodes.Ldloca : OpCodes.Ldloc, local);
        else
            throw new NotSupportedException();

        // we've handled reference conversion
        _byReference = false;

        return node;
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitUnary(LinqExpressions.UnaryExpression node)
    {
        // emit the operand
        Visit(node.Operand);

        // emit code to perform the operation
        switch (node.NodeType)
        {
            case LinqExpressions.ExpressionType.Convert:
                _ilGenerator.Emit(OpCodes.Castclass, node.Type);
                break;

            case LinqExpressions.ExpressionType.Not:
                if (node.Operand.Type == typeof(bool))
                {
                    _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    _ilGenerator.Emit(OpCodes.Xor);
                }
                else
                {
                    _ilGenerator.Emit(OpCodes.Not);
                }
                break;

            case LinqExpressions.ExpressionType.TypeAs:
                _ilGenerator.Emit(OpCodes.Isinst, node.Type);
                break;

            default:
                throw new NotImplementedException();
        }

        return node;
    }

    /// <summary>
    /// Compile the given <see cref="LinqExpressions.Expression"/>.
    /// </summary>
    /// <param name="expression">The expression to compile.</param>
    public void Compile(LinqExpressions.Expression expression)
    {
        var simplifiedExpression = ExpressionSimplifier.Instance.Visit(expression);
        Visit(simplifiedExpression);
        _ilGenerator.Emit(OpCodes.Ret);
    }
}
