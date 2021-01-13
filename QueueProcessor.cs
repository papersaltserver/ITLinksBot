using System;
using System.Collections.Generic;
using ItLinksBot.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ItLinksBot
{
    public static class QueueProcessor
    {
        private const int tgMessageSizeLimit = 4080;
        private static List<string> splitMessageForTg(string message)
        {
            List<string> messageChunks = new List<string>();
            int telegramMessageLimit = tgMessageSizeLimit;
            
            if (message.Length > telegramMessageLimit)
            {
                int i = 0;
                while (i < message.Length)
                {
                    if (i + telegramMessageLimit > message.Length)
                    {
                        telegramMessageLimit = message.Length - i;
                        if (i > 0)
                        {
                            messageChunks.Add("[...]" + message.Substring(i, telegramMessageLimit));
                        }
                        else
                        {
                            messageChunks.Add(message.Substring(i, telegramMessageLimit));
                        }
                        i += telegramMessageLimit;
                    }
                    else
                    {
                        var currentChunk = message.Substring(i, telegramMessageLimit);
                        char[] charArray = currentChunk.ToCharArray();
                        Array.Reverse(charArray);
                        var reverseChunk = new string(charArray);
                        //trying to break string by new line
                        var closestBreak = reverseChunk.IndexOf("\n", 0, (int)message.Length / 3);
                        if (closestBreak >= 0)
                        {
                            if (i > 0)
                            {
                                messageChunks.Add("[...]" + message.Substring(i, telegramMessageLimit - closestBreak) + "[...]");
                            }
                            else
                            {
                                messageChunks.Add(message.Substring(i, telegramMessageLimit - closestBreak) + "[...]");
                            }
                            i += telegramMessageLimit - closestBreak;
                            continue;
                        }
                        //if no new line found trying to break by dot
                        var closestDot = reverseChunk.IndexOf(".", 0, (int)message.Length / 3);
                        if (closestDot >= 0)
                        {
                            if (i > 0)
                            {
                                messageChunks.Add("[...]" + message.Substring(i, telegramMessageLimit - closestDot) + "[...]");
                            }
                            else
                            {
                                messageChunks.Add(message.Substring(i, telegramMessageLimit - closestDot) + "[...]");
                            }
                            i += telegramMessageLimit - closestDot;
                            continue;
                        }
                        //if no dot found trying to break by space
                        var closestSpace = reverseChunk.IndexOf(" ", 0, (int)message.Length / 3);
                        if (closestSpace >= 0)
                        {
                            if (i > 0)
                            {
                                messageChunks.Add("[...]" + message.Substring(i, telegramMessageLimit - closestSpace) + "[...]");
                            }
                            else
                            {
                                messageChunks.Add(message.Substring(i, telegramMessageLimit - closestSpace) + "[...]");
                            }
                            i += telegramMessageLimit - closestSpace;
                            continue;
                        }
                        //worst case scenario - no new line, no break, no space found
                        if (i > 0)
                        {
                            messageChunks.Add("[...]" + message.Substring(i, telegramMessageLimit) + "[...]");
                        }
                        else
                        {
                            messageChunks.Add(message.Substring(i, telegramMessageLimit) + "[...]");
                        }
                        i += telegramMessageLimit;
                    }
                }
            }
            else
            {
                messageChunks.Add(message);
            }
            return messageChunks;
        }
        public static List<DigestPost> AddDigestPost(TelegramChannel tgChannel, Digest digest, TelegramAPI bot)
        {
            var parser = ParserFactory.Setup(tgChannel.Provider);
            string message = parser.FormatDigestPost(digest).Replace("&nbsp;", " ");
            List<DigestPost> responses = new List<DigestPost>();
            var messageChunks = splitMessageForTg(message);

            int j = 0;
            while (j < messageChunks.Count)
            {
                string chunk = messageChunks[j];
                var linkPostResult = bot.SendMessage(tgChannel.ChannelName, chunk);
                var botPostObject = JObject.Parse(linkPostResult);
                if ((bool)botPostObject["ok"])
                {
                    responses.Add(new DigestPost
                    {
                        Channel = tgChannel,
                        Digest = digest,
                        TelegramMessageID = (int)botPostObject["result"]["message_id"],
                        PostDate = Utils.UnixTimeStampToDateTime((int)botPostObject["result"]["date"]),
                        PostLink = string.Format("https://t.me/{0}/{1}", (string)botPostObject["result"]["chat"]["username"], (string)botPostObject["result"]["message_id"]),
                        PostText = (string)botPostObject["result"]["text"]
                    });
                    j++;
                }
                else if ((int)botPostObject["error_code"] == 429)
                {
                    var secondsToSleep = (int)botPostObject["parameters"]["retry_after"];
                    Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.ChannelName} again");
                    System.Threading.Thread.Sleep((secondsToSleep + 2)*1000);
                }
                else
                {
                    Log.Error("Error from posting message to Telegram API {linkPostResult}", linkPostResult);
                    throw new Exception($"Unknown service response: {linkPostResult}");
                }
            }
            return responses;
        }

        public static List<LinkPost> AddLinkPost(TelegramChannel tgChannel, Link link, TelegramAPI bot)
        {
            var parser = ParserFactory.Setup(tgChannel.Provider);
            string message = parser.FormatLinkPost(link).Replace("&nbsp;", " ");
            List<LinkPost> responses = new List<LinkPost>();
            var messageChunks = splitMessageForTg(message);

            int j = 0;
            while(j<messageChunks.Count)
            {
                string chunk = messageChunks[j];
                var linkPostResult = bot.SendMessage(tgChannel.ChannelName, chunk);
                var linkPostObject = JObject.Parse(linkPostResult);
                if ((bool)linkPostObject["ok"])
                {
                    responses.Add(new LinkPost
                    {
                        Channel = tgChannel,
                        Link = link,
                        TelegramMessageID = (int)linkPostObject["result"]["message_id"],
                        PostDate = Utils.UnixTimeStampToDateTime((int)linkPostObject["result"]["date"]),
                        PostLink = string.Format("https://t.me/{0}/{1}", (string)linkPostObject["result"]["chat"]["username"], (string)linkPostObject["result"]["message_id"]),
                        PostText = (string)linkPostObject["result"]["text"]
                    });
                    j++;
                }
                else if ((int)linkPostObject["error_code"] == 429)
                {
                    var secondsToSleep = (int)linkPostObject["parameters"]["retry_after"];
                    Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.ChannelName} again");
                    System.Threading.Thread.Sleep((secondsToSleep + 2)*1000);
                }
                else
                {
                    Log.Error("Error from posting message to Telegram API {linkPostResult}", linkPostResult);
                    throw new Exception($"Unknown service response: {linkPostResult}");
                }
            }
            return responses;
        }
    }
}