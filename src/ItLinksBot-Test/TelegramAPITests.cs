using Xunit;
using ItLinksBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItLinksBot.DTO;
using ItLinksBot.TelegramDTO;
using Newtonsoft.Json.Linq;

namespace ItLinksBot_Tests
{
    public class TelegramAPITests
    {
        [Fact()]
        public void Send2ImagesGroupTest()
        {
            var file1string = "iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAIAAAAC64paAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABCSURBVDhPY/hPARiOmm9PsGJI2wblYAF4NG9LYwAC0jWDrGRgsEpLI8fm29u23QZR5Dt7VDM2QDvNhMCoZpLA//8AuCWCOX3YuogAAAAASUVORK5CYII=";
            var file2string = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAUCAIAAAALACogAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAB4SURBVDhP5ZLRDYAgDAWLY+E+uA4ug8PgLmgR9RWL0U/j/UDIu7ZpMCklekNXzsd8QZjH3hSGqbwJ1i0BwRG5cFytj/kOSGENnZno7S4DVQcEugFNgesrEzUETivVGUXIaa14phZuam9IoTU4IATei+Ta64ffm2gBStd+3jeYNC0AAAAASUVORK5CYII=";
            var file1bytes = Convert.FromBase64String(file1string);
            var file2bytes = Convert.FromBase64String(file2string);
            var dtoArray = new PhotoDTO[] {
                new PhotoDTO {
                    FileName = "1.png",
                    ContentBytes = file1bytes
                },
                new PhotoDTO
                {
                    FileName = "2.png",
                    ContentBytes = file2bytes
                }
            };
            string Channel = Environment.GetEnvironmentVariable("TELEGRAM_CHANNELID");
            var botKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
            TelegramAPI bot = new(botKey);
            var telegramMedias = new TelegramPhoto[] { 
                new TelegramPhoto
                {
                    caption = "Caption test",
                    media = "attach://1.png"
                },
                new TelegramPhoto
                {
                    media = "attach://2.png"
                }
            };
            var resp = bot.SendMediaGroup(Channel, telegramMedias, dtoArray);
            var botPostObject = JObject.Parse(resp);
            Assert.True((bool)botPostObject["ok"], $"Error sending 2 files, error:\n{resp}");
        }
        [Fact()]
        public void SendOneImageTest()
        {
            var file1string = "iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAIAAAAC64paAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABCSURBVDhPY/hPARiOmm9PsGJI2wblYAF4NG9LYwAC0jWDrGRgsEpLI8fm29u23QZR5Dt7VDM2QDvNhMCoZpLA//8AuCWCOX3YuogAAAAASUVORK5CYII=";
            var file1bytes = Convert.FromBase64String(file1string);
            var dtoArray = new PhotoDTO[] {
                new PhotoDTO {
                    FileName = "1.png",
                    ContentBytes = file1bytes
                }
            };
            string Channel = Environment.GetEnvironmentVariable("TELEGRAM_CHANNELID");
            var botKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
            
            TelegramAPI bot = new(botKey);
            var telegramMedias = new TelegramPhoto[] {
                new TelegramPhoto
                {
                    caption = "Caption test",
                    media = "attach://1.png"
                }
            };
            var resp = bot.SendMediaGroup(Channel, telegramMedias, dtoArray);
            var botPostObject = JObject.Parse(resp);
            Assert.True((bool)botPostObject["ok"], $"Error sending 1 file, error:\n{resp}");
        }
    }
}