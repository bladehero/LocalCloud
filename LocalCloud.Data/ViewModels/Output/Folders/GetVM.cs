using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LocalCloud.Data.ViewModels.Output.Folders
{
    public class GetVM : ResultVM<IEnumerable<string>>
    {
        [JsonPropertyName("names")]
        public override IEnumerable<string> Data { get => base.Data; set => base.Data = value; }
    }
}
