namespace CleanArq.Infra.Identity;

public sealed class KeycloakProvisioningOptions
{
    public const string SectionName = "Keycloak:Provisioning";
    public string ServerUrl { get; set; } = "http://localhost:8180";
    public string Realm { get; set; } = "CleanArq";
    public string ClientId { get; set; } = "cleanarq-user-provisioning";
    public string ClientSecret { get; set; } = string.Empty;
    public bool AllowHttpForDevelopment { get; set; }
}
