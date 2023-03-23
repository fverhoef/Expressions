using LinqExpressions = System.Linq.Expressions;

namespace Expressions.Utilities;

/// <summary>
/// Helper class which provides calls an overridable generic method when no override was made for a specific node type.
/// </summary>
public abstract class ExpressionVisitor : LinqExpressions.ExpressionVisitor
{
    /// <summary>
    /// Visit an unhandled node.
    /// </summary>
    /// <param name="node">Node to visit.</param>
    protected abstract void VisitUnhandled(object node);

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitBinary(LinqExpressions.BinaryExpression node)
    {
        VisitUnhandled(node);
        return base.VisitBinary(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitBlock(LinqExpressions.BlockExpression node)
    {
        VisitUnhandled(node);
        return base.VisitBlock(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.CatchBlock VisitCatchBlock(LinqExpressions.CatchBlock node)
    {
        VisitUnhandled(node);
        return base.VisitCatchBlock(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitConditional(LinqExpressions.ConditionalExpression node)
    {
        VisitUnhandled(node);
        return base.VisitConditional(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitConstant(LinqExpressions.ConstantExpression node)
    {
        VisitUnhandled(node);
        return base.VisitConstant(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitDebugInfo(LinqExpressions.DebugInfoExpression node)
    {
        VisitUnhandled(node);
        return base.VisitDebugInfo(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitDefault(LinqExpressions.DefaultExpression node)
    {
        VisitUnhandled(node);
        return base.VisitDefault(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitDynamic(LinqExpressions.DynamicExpression node)
    {
        VisitUnhandled(node);
        return base.VisitDynamic(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.ElementInit VisitElementInit(LinqExpressions.ElementInit node)
    {
        VisitUnhandled(node);
        return base.VisitElementInit(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitExtension(LinqExpressions.Expression node)
    {
        VisitUnhandled(node);
        return base.VisitExtension(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitGoto(LinqExpressions.GotoExpression node)
    {
        VisitUnhandled(node);
        return base.VisitGoto(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitIndex(LinqExpressions.IndexExpression node)
    {
        VisitUnhandled(node);
        return base.VisitIndex(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitInvocation(LinqExpressions.InvocationExpression node)
    {
        VisitUnhandled(node);
        return base.VisitInvocation(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitLabel(LinqExpressions.LabelExpression node)
    {
        VisitUnhandled(node);
        return base.VisitLabel(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.LabelTarget VisitLabelTarget(LinqExpressions.LabelTarget node)
    {
        VisitUnhandled(node);
        return base.VisitLabelTarget(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitLambda<T>(LinqExpressions.Expression<T> node)
    {
        VisitUnhandled(node);
        return base.VisitLambda<T>(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitListInit(LinqExpressions.ListInitExpression node)
    {
        VisitUnhandled(node);
        return base.VisitListInit(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitLoop(LinqExpressions.LoopExpression node)
    {
        VisitUnhandled(node);
        return base.VisitLoop(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitMember(LinqExpressions.MemberExpression node)
    {
        VisitUnhandled(node);
        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.MemberAssignment VisitMemberAssignment(LinqExpressions.MemberAssignment node)
    {
        VisitUnhandled(node);
        return base.VisitMemberAssignment(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitMemberInit(LinqExpressions.MemberInitExpression node)
    {
        VisitUnhandled(node);
        return base.VisitMemberInit(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.MemberListBinding VisitMemberListBinding(LinqExpressions.MemberListBinding node)
    {
        VisitUnhandled(node);
        return base.VisitMemberListBinding(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.MemberMemberBinding VisitMemberMemberBinding(LinqExpressions.MemberMemberBinding node)
    {
        VisitUnhandled(node);
        return base.VisitMemberMemberBinding(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitMethodCall(LinqExpressions.MethodCallExpression node)
    {
        VisitUnhandled(node);
        return base.VisitMethodCall(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitNew(LinqExpressions.NewExpression node)
    {
        VisitUnhandled(node);
        return base.VisitNew(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitNewArray(LinqExpressions.NewArrayExpression node)
    {
        VisitUnhandled(node);
        return base.VisitNewArray(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitParameter(LinqExpressions.ParameterExpression node)
    {
        VisitUnhandled(node);
        return base.VisitParameter(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitRuntimeVariables(LinqExpressions.RuntimeVariablesExpression node)
    {
        VisitUnhandled(node);
        return base.VisitRuntimeVariables(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitSwitch(LinqExpressions.SwitchExpression node)
    {
        VisitUnhandled(node);
        return base.VisitSwitch(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.SwitchCase VisitSwitchCase(LinqExpressions.SwitchCase node)
    {
        VisitUnhandled(node);
        return base.VisitSwitchCase(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitTry(LinqExpressions.TryExpression node)
    {
        VisitUnhandled(node);
        return base.VisitTry(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitTypeBinary(LinqExpressions.TypeBinaryExpression node)
    {
        VisitUnhandled(node);
        return base.VisitTypeBinary(node);
    }

    /// <inheritdoc/>
    protected override LinqExpressions.Expression VisitUnary(LinqExpressions.UnaryExpression node)
    {
        VisitUnhandled(node);
        return base.VisitUnary(node);
    }
}

