namespace PlatformPlatform.SharedKernel.StronglyTypedIds;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IdPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}
