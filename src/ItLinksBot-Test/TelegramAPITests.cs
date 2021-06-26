﻿using Xunit;
using ItLinksBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItLinksBot.DTO;
using ItLinksBot.TelegramDTO;

namespace ItLinksBot_Tests
{
    public class TelegramAPITests
    {
        [Fact()]
        public void SendMediaGroupTest()
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
            string Channel = "-1001380515454";
            TelegramAPI bot = new("1442631783:AAE5BtVd_JLFTs0-r3hJZqACAKIwej-7Q5M");
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
            Assert.True(false, "This test needs an implementation");
        }
    }
}