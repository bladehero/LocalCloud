namespace LocalCloud.Data.ViewModels
{
    public class ResultVM<T> : ResultVM
    {
        public virtual T Data { get; set; } = default;
    }
    public class ResultVM
    {
        public const string UnexpectedError = "An unexpected error was occured!";

        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;

        public virtual void SetUnexpectedError() => Message = UnexpectedError;
    }
}
