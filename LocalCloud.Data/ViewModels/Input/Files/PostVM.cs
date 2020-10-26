using Microsoft.AspNetCore.Http;

namespace LocalCloud.Data.ViewModels.Input.Files
{    
    public class PostVM
    {
        public string Path { get; set; }
        public IFormFile File { get; set; }
    }
}
