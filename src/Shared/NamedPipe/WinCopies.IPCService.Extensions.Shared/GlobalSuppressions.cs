// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

using static GlobalSuppressionsConsts;

[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = ValidatedThroughWinCopiesUtilitiesMethods, Scope = "member", Target = "~M:WinCopies.IPCService.Extensions.Extensions.StartInstanceAsync``3(WinCopies.IPCService.Extensions.ISingleInstanceApp{``0,``2})~System.Threading.Tasks.Task{System.ValueTuple{System.Threading.Mutex,System.Boolean,WinCopies.NullableGeneric{``2}}}")]

internal static class GlobalSuppressionsConsts
{
    public const string ValidatedThroughWinCopiesUtilitiesMethods = "Validated through WinCopies utilities methods.";
}
