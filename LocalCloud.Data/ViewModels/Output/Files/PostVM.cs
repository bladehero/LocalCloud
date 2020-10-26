using System.Text.Json.Serialization;

namespace LocalCloud.Data.ViewModels.Output.Files
{
    public class PostVM : ResultVM<string>
    {
        [JsonPropertyName("path")]
        public override string Data { get => base.Data; set => base.Data = value; }
    }
}
