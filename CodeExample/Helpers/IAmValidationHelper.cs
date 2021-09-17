namespace TRM.Web.Helpers
{
    public interface IAmValidationHelper
    {
        bool IsValid<T>(T model, out string resultMsg);

        bool IsValid<T>(T model);
    }
}