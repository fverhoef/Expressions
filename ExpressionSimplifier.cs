using LinqExpressions = System.Linq.Expressions;

namespace Expressions.Utilities;

/// <summary>
/// Simplifies an expression tree so it can be more easily compiled.
/// </summary>
internal class ExpressionSimplifier : LinqExpressions.ExpressionVisitor
{
    public static readonly ExpressionSimplifier Instance = new ExpressionSimplifier();

    private MethodInfo GetOverloadedOperator(string name, Type[] operandTypes, Type returnType)
    {
        foreach (var type in operandTypes.Concat(returnType != null ? Enumerable.Repeat(returnType, 1) : Enumerable.Empty<Type>()))
        {
            // look for a method on this type
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(m => m.IsSpecialName &&
                                                  m.Name == name &&
                                                  m.GetParameters().Length == operandTypes.Length &&
                                                  m.GetParameters().Select((p, n) => p.ParameterType.IsAssignableFrom(operandTypes[n])).All(t => t) &&
                                                  (returnType == null || m.ReturnType == returnType));
            if (method != null)
                return method;
        }

        return null;
    }

    protected override LinqExpressions.Expression VisitBinary(LinqExpressions.BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case LinqExpressions.ExpressionType.Assign:
                if (node.Left is LinqExpressions.MemberExpression && ((LinqExpressions.MemberExpression)node.Left).Member is PropertyInfo)
                {
                    // simplify property assignment to setter invocation
                    var member = (LinqExpressions.MemberExpression)node.Left;
                    var property = (PropertyInfo)member.Member;
                    return Visit(member.Expression != null ? LinqExpressions.Expression.Call(member.Expression, property.GetSetMethod(nonPublic: true), node.Right) :
                                                             LinqExpressions.Expression.Call(property.GetSetMethod(nonPublic: true), node.Right));
                }
                else if (node.Left is LinqExpressions.IndexExpression && ((LinqExpressions.IndexExpression)node.Left).Indexer != null)
                {
                    // simplify indexer assignment to setter invocation
                    var index = (LinqExpressions.IndexExpression)node.Left;
                    var property = index.Indexer;
                    return Visit(LinqExpressions.Expression.Call(index.Object, property.GetSetMethod(nonPublic: true), index.Arguments.Concat(new[] { node.Right })));
                }
                break;

            case LinqExpressions.ExpressionType.Coalesce:
                {
                    // simplify coalesce to its component operations
                    var temporaryVariable = LinqExpressions.Expression.Variable(node.Left.Type);
                    return Visit(
                        LinqExpressions.Expression.Block(
                            new LinqExpressions.ParameterExpression[] { temporaryVariable },
                            LinqExpressions.Expression.Assign(temporaryVariable, node.Left),
                            LinqExpressions.Expression.Condition(
                                LinqExpressions.Expression.NotEqual(temporaryVariable, LinqExpressions.Expression.Constant(null, node.Left.Type)),
                                LinqExpressions.Expression.Convert(temporaryVariable, node.Right.Type),
                                node.Right)));
                }

            case LinqExpressions.ExpressionType.Equal:
            case LinqExpressions.ExpressionType.NotEqual:
                {
                    // simplify comparisons between nullable types
                    if (node.Left.Type.IsNullable() && node.Right.Type.IsNullable())
                    {
                        LinqExpressions.Expression expression;

                        // if we're comparing to null, we can simplify to a check on HasValue
                        if (node.Left is LinqExpressions.ConstantExpression && ((LinqExpressions.ConstantExpression)node.Left).Value == null)
                            expression = LinqExpressions.Expression.Property(node.Right, "HasValue");
                        else if (node.Right is LinqExpressions.ConstantExpression && ((LinqExpressions.ConstantExpression)node.Right).Value == null)
                            expression = LinqExpressions.Expression.Property(node.Left, "HasValue");
                        else
                        {
                            // replace with a call to Nullable.Equals<T>
                            expression = LinqExpressions.Expression.Call(typeof(Nullable).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static)
                                                                                         .MakeGenericMethod(Nullable.GetUnderlyingType(node.Left.Type)), node.Left, node.Right);

                            if (node.NodeType == LinqExpressions.ExpressionType.NotEqual)
                                expression = LinqExpressions.Expression.Not(expression);
                        }

                        if (node.NodeType == LinqExpressions.ExpressionType.Equal && expression is LinqExpressions.MemberExpression)
                            expression = LinqExpressions.Expression.Not(expression);

                        return Visit(expression);
                    }

                    // simplify overloaded operators to method invocations
                    var method = node.NodeType == LinqExpressions.ExpressionType.Equal ? GetOverloadedOperator("op_Equality", new Type[] { node.Left.Type, node.Right.Type }, null) :
                                                                                         GetOverloadedOperator("op_Inequality", new Type[] { node.Left.Type, node.Right.Type }, null);
                    if (method != null)
                        return Visit(LinqExpressions.Expression.Call(method, node.Left, node.Right));
                }
                break;
        }

        return base.VisitBinary(node);
    }

    protected override LinqExpressions.Expression VisitIndex(LinqExpressions.IndexExpression node)
    {
        if (node.Indexer != null)
            return Visit(LinqExpressions.Expression.Call(node.Object, node.Indexer.GetGetMethod(nonPublic: true), node.Arguments));

        return base.VisitIndex(node);
    }

    protected override LinqExpressions.Expression VisitMember(LinqExpressions.MemberExpression node)
    {
        // simplify property access to getter invocation
        if (node.Member is PropertyInfo)
        {
            var property = (PropertyInfo)node.Member;
            return Visit(node.Expression != null ? LinqExpressions.Expression.Call(node.Expression, property.GetGetMethod(nonPublic: true)) :
                                                   LinqExpressions.Expression.Call(property.GetGetMethod(nonPublic: true)));
        }

        return base.VisitMember(node);
    }

    protected override LinqExpressions.Expression VisitMemberInit(LinqExpressions.MemberInitExpression node)
    {
        // simplify member init expressions to their component operations
        var temporaryVariable = LinqExpressions.Expression.Variable(node.Type);

        return Visit(
            LinqExpressions.Expression.Block(
                new LinqExpressions.ParameterExpression[] { temporaryVariable },
                Enumerable.Repeat<LinqExpressions.Expression>(LinqExpressions.Expression.Assign(temporaryVariable, node.NewExpression), 1)
                          .Concat(node.Bindings
                                      .OfType<LinqExpressions.MemberAssignment>()
                                      .Select(b => LinqExpressions.Expression.Assign(LinqExpressions.Expression.MakeMemberAccess(temporaryVariable, b.Member), b.Expression)))
                          .Concat(Enumerable.Repeat(temporaryVariable, 1))));
    }

    protected override LinqExpressions.Expression VisitUnary(LinqExpressions.UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case LinqExpressions.ExpressionType.Convert:
                // remove useless conversions
                if (node.Type == node.Operand.Type)
                    return Visit(node.Operand);

                // attempt to simplify a conversion to a method call
                if (node.Method != null)
                {
                    if (node.Operand.Type.IsNullable() && node.Type.IsNullable() && !node.Method.ReturnType.IsNullable())
                        // simplify nullable conversions
                        return Visit(LinqExpressions.Expression.IfThenElse(
                            LinqExpressions.Expression.Equal(node.Operand, LinqExpressions.Expression.Constant(null, node.Operand.Type)),
                            LinqExpressions.Expression.Convert(LinqExpressions.Expression.Call(node.Method, node.Operand), node.Type),
                            LinqExpressions.Expression.Constant(null, node.Type)));
                    else
                        // simplify conversions with a method to a method invocation
                        return Visit(LinqExpressions.Expression.Call(node.Method, node.Operand));
                }
                if (node.Operand.Type.IsNullable() && !node.Type.IsNullable() && node.Type.Equals(node.Operand.Type.MakeNonNullable()))
                {
                    // simplify conversions from nullable to non-nullable
                    return Visit(LinqExpressions.Expression.Property(node.Operand, "Value"));
                }
                if (!node.Operand.Type.IsNullable() && node.Type.IsNullable() && node.Operand.Type.Equals(node.Type.MakeNonNullable()))
                {
                    // simplify conversions from non-nullable to nullable
                    return Visit(LinqExpressions.Expression.New(node.Type.GetConstructor(new Type[] { node.Type.MakeNonNullable() }), node.Operand));
                }
                else
                {
                    // simplify overloaded conversions to method invocations
                    var method = GetOverloadedOperator("op_Explicit", new Type[] { node.Operand.Type }, node.Type) ??
                                 GetOverloadedOperator("op_Implicit", new Type[] { node.Operand.Type }, node.Type);
                    if (method != null)
                        return Visit(LinqExpressions.Expression.Call(method, node.Operand));
                }
                break;
        }

        return base.VisitUnary(node);
    }
}
