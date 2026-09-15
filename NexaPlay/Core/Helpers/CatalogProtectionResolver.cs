namespace NexaPlay.Core.Helpers;

public static class CatalogProtectionResolver
{
    public static bool Resolve(bool baseProtection, bool protectedSourceMember, bool? runtimeOverride) =>
        runtimeOverride ?? (baseProtection || protectedSourceMember);
}
