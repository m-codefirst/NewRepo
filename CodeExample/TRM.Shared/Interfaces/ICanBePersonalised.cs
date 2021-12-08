using TRM.Shared.Constants;

namespace TRM.Shared.Interfaces
{
    public interface ICanBePersonalised
    {
        Enums.CanBePersonalised CanBePersonalised { get; set; }
        Enums.PersonalisationType PersonalisationType { get; set; }
        string PersonalisationPrice { get; set; }
        string PersonalisedItemNumber { get; set; }
    }
}
