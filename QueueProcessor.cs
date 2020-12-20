using System;
using ItLinksBot.Models;
using Newtonsoft.Json.Linq;

namespace ItLinksBot
{
    public static class QueueProcessor
    {
        public static DigestPost AddDigestPost(TelegramChannel tgChannel, Digest digest, TelegramAPI bot)
        {
            var parser = ParserFactory.Setup(tgChannel.Provider);
            var botPostResult = bot.SendMessage(tgChannel.ChannelName, parser.FormatDigestPost(digest));
            var botPostObject = JObject.Parse(botPostResult);


            if ((bool)botPostObject["ok"])
            {
                return new DigestPost
                {
                    Channel = tgChannel,
                    Digest = digest,
                    TelegramMessageID = (int)botPostObject["result"]["message_id"],
                    PostDate = Utils.UnixTimeStampToDateTime((int)botPostObject["result"]["date"]),
                    PostLink = string.Format("https://t.me/{0}/{1}", (string)botPostObject["result"]["chat"]["username"], (string)botPostObject["result"]["message_id"]),
                    PostText = (string)botPostObject["result"]["text"]
                };
            }
            else if ((int)botPostObject["error_code"] == 429)
            {
                //"Too Many Requests"
                return null;
            }
            else
            {
                throw new Exception($"Unknown service response: {botPostResult}");
            }
        }

        public static LinkPost AddLinkPost(TelegramChannel tgChannel, Link link, TelegramAPI bot)
        {
            var parser = ParserFactory.Setup(tgChannel.Provider);
            var linkPostResult = bot.SendMessage(tgChannel.ChannelName, parser.FormatLinkPost(link));
            var linkPostObject = JObject.Parse(linkPostResult);
            if ((bool)linkPostObject["ok"])
            {
                return new LinkPost
                {
                    Channel = tgChannel,
                    Link = link,
                    TelegramMessageID = (int)linkPostObject["result"]["message_id"],
                    PostDate = Utils.UnixTimeStampToDateTime((int)linkPostObject["result"]["date"]),
                    PostLink = string.Format("https://t.me/{0}/{1}", (string)linkPostObject["result"]["chat"]["username"], (string)linkPostObject["result"]["message_id"]),
                    PostText = (string)linkPostObject["result"]["text"]
                };
            }
            else if ((int)linkPostObject["error_code"] == 429)
            {
                //"Too Many Requests"
                return null;
            }
            else
            {
                throw new Exception($"Unknown service response: {linkPostResult}");
            }
        }
    }
}