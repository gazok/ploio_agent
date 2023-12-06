namespace Frouros.Proxy.Models.Web;

public enum ModuleActivation
{
    Enabled,
    Disabled
}

public record ModuleActivationInfo(
    Guid GUID, 
    ModuleActivation Activation
);
