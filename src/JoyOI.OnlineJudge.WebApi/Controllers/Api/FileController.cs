using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JoyOI.ManagementService.SDK;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class FileController : BaseController
    {
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload([FromServices] ManagementServiceClient mgmt)
        {
            var context = HttpContext;
            var file = context.Request.Form.Files["file"];
            if (file != null)
            {
                var id = await mgmt.PutBlobAsync("joyoi_online_judge_upload_" + file.FileName, file.ReadAllBytes());
                var f = new
                {
                    Id = id,
                    Time = DateTime.Now,
                    ContentType = file.ContentType,
                    ContentLength = file.Length,
                    FileName = file.GetFileName(),
                    Bytes = file.ReadAllBytes()
                };

                return Json(f);
            }
            else
            {
                var blob = new Base64StringFile(context.Request.Form["file"]);
                var id = await mgmt.PutBlobAsync("joyoi_online_judge_upload_file", blob.AllBytes);

                var f = new
                {
                    Id = id,
                    Time = DateTime.Now,
                    ContentType = blob.ContentType,
                    ContentLength = blob.Base64String.Length,
                    FileName = "file",
                    Bytes = blob.AllBytes
                };

                return Json(f);
            }
        }

        [HttpGet("Download/{id:Guid}")]
        public async Task<IActionResult> Download(Guid id, [FromServices] ManagementServiceClient mgmt)
        {
            var blob = await mgmt.GetBlobAsync(id);
            if (blob.Name.StartsWith("joyoi_online_judge_upload_"))
            {
                return File(blob.Body, "application/octet-stream", blob.Name);
            }
            else
            {
                Response.StatusCode = 404;
                return Content("Not Found");
            }
        }
    }
}
