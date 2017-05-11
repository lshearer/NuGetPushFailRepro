namespace NugetPushIssueRepro.Utility
{
    internal interface IAccessor<TValue>
    {
        TValue Value { get; }
        void SetValue(TValue value);
    }
}