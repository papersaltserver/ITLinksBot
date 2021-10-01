using AutoMapper;
using ItLinksBot.DTO;
using ItLinksBot.Models;
using ItLinksBot.TelegramDTO;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ItLinksBot
{
    public static class QueueProcessor
    {
        private static string DecorateTelegramString(string message, bool isFirst, bool isLast)
        {
            StringBuilder tempMessage = new(message);
            if (!isFirst)
            {
                tempMessage.Insert(0, "[...]");
            }
            if (!isLast)
            {
                tempMessage.Append("[...]");
            }
            return tempMessage.ToString();
        }
        private const int tgMessageSizeLimit = 4080;
        private const int tgCaptionSizeLimit = 1010;
        private static List<string> SplitCaptionForTg(string message)
        {
            List<string> messageChunks = new();
            int telegramCaptionLimit = tgCaptionSizeLimit;

            if (message.Length > telegramCaptionLimit)
            {
                var currentChunk = message.Substring(0, telegramCaptionLimit);
                char[] charArray = currentChunk.ToCharArray();
                Array.Reverse(charArray);
                var reverseChunk = new string(charArray);
                int splitPosition;
                //trying to break string by new line in the last third of block
                var closestBreak = reverseChunk.IndexOf("\n", 0, reverseChunk.Length / 3);
                //if no new line found trying to break by dot  in the last third of block
                var closestDot = reverseChunk.IndexOf(".", 0, reverseChunk.Length / 3);
                //if no dot found trying to break by space
                var closestSpace = reverseChunk.IndexOf(" ", 0, reverseChunk.Length / 3);

                if (closestBreak >= 0)
                {
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit - closestBreak), true, false));
                    splitPosition = tgCaptionSizeLimit - closestBreak;
                }
                else if (closestDot >= 0)
                {
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit - closestDot), true, false));
                    splitPosition = tgCaptionSizeLimit - closestDot;
                }
                else if (closestSpace >= 0)
                {
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit - closestSpace), true, false));
                    splitPosition = tgCaptionSizeLimit - closestSpace;
                }
                else
                {
                    //worst case scenario - no new line, no break, no space found
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit), true, false));
                    splitPosition = tgCaptionSizeLimit;
                }
                var restChunks = SplitMessageForTg("[...]" + message.Substring(0, splitPosition));
                messageChunks.AddRange(restChunks);
            }
            else
            {
                messageChunks.Add(message);
            }
            return messageChunks;
        }
        private static List<string> SplitMessageForTg(string message)
        {
            List<string> messageChunks = new();
            int telegramMessageLimit = tgMessageSizeLimit;

            if (message.Length > telegramMessageLimit)
            {
                int i = 0;
                while (i < message.Length)
                {
                    if (i + telegramMessageLimit > message.Length)
                    {
                        telegramMessageLimit = message.Length - i;
                        messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit), i == 0, true));
                        i += telegramMessageLimit;
                    }
                    else
                    {
                        var currentChunk = message.Substring(i, telegramMessageLimit);
                        char[] charArray = currentChunk.ToCharArray();
                        Array.Reverse(charArray);
                        var reverseChunk = new string(charArray);
                        //trying to break string by new line
                        var closestBreak = reverseChunk.IndexOf("\n", 0, reverseChunk.Length / 3);
                        if (closestBreak >= 0)
                        {
                            messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit - closestBreak), i == 0, false));
                            i += telegramMessageLimit - closestBreak;
                            continue;
                        }
                        //if no new line found trying to break by dot
                        var closestDot = reverseChunk.IndexOf(".", 0, reverseChunk.Length / 3);
                        if (closestDot >= 0)
                        {
                            messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit - closestDot), i == 0, false));
                            i += telegramMessageLimit - closestDot;
                            continue;
                        }
                        //if no dot found trying to break by space
                        var closestSpace = reverseChunk.IndexOf(" ", 0, reverseChunk.Length / 3);
                        if (closestSpace >= 0)
                        {
                            messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit - closestSpace), i == 0, false));
                            i += telegramMessageLimit - closestSpace;
                            continue;
                        }
                        //worst case scenario - no new line, no break, no space found
                        messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit), i == 0, false));
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

        public static List<DigestPost> AddDigestPost(TelegramChannel tgChannel, Digest digest, TelegramAPI bot, IServiceProvider serviceProvider)
        {
            IEnumerable<IParser> serviceCollection = serviceProvider.GetServices<IParser>();
            var parser = serviceCollection.FirstOrDefault(p => p.CurrentProvider == tgChannel.Provider.ProviderName);
            string message = HtmlEntityText.ToHtmlCode(parser.FormatDigestPost(digest));
            List<DigestPost> responses = new();
            var messageChunks = SplitMessageForTg(message);

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
                    Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.Provider.ProviderName} again");
                    System.Threading.Thread.Sleep((secondsToSleep + 2) * 1000);
                }
                else
                {
                    Log.Error("Error from posting message to Telegram API {linkPostResult}", linkPostResult);
                    throw new Exception($"Unknown service response: {linkPostResult}");
                }
            }
            return responses;
        }

        public static List<LinkPost> AddLinkPost(TelegramChannel tgChannel, Link link, TelegramAPI bot, IServiceProvider serviceProvider)
        {
            IEnumerable<IParser> serviceCollection = serviceProvider.GetServices<IParser>();
            IMapper mapper = serviceProvider.GetService<IMapper>();
            var parser = serviceCollection.FirstOrDefault(p => p.CurrentProvider == tgChannel.Provider.ProviderName);
            string message = HtmlEntityText.ToHtmlCode(parser.FormatLinkPost(link));
            List<string> messageChunks = new();
            List<LinkPost> responses = new();
            int numberPostedChunks = 0;
            if (link.Medias != null && link.Medias.Any())
            {
                messageChunks = SplitCaptionForTg(message);
                while (numberPostedChunks < 1)
                {
                    string chunk = messageChunks[0];
                    var mediasToPost = link.Medias;
                    ITelegramMedia[] telegramMedia = new ITelegramMedia[mediasToPost.Count];
                    IMediaDTO[] mediaDTOs = new IMediaDTO[mediasToPost.Count];
                    if (mediasToPost.Count > 10)
                    {
                        //Telegram group supports up to 10 medias in 1 group
                        throw new NotImplementedException();
                    }
                    int mediaIndex = 0;
                    foreach (var m in mediasToPost)
                    {
                        string currentCaption;
                        if (mediaIndex == 0)
                        {
                            currentCaption = chunk;
                        }
                        else
                        {
                            currentCaption = "";
                        }
                        switch (m.GetType().Name)
                        {
                            case "Photo":
                                telegramMedia[mediaIndex] = new TelegramPhoto
                                {
                                    caption = currentCaption,
                                    media = $"attach://{m.FileName}"
                                };
                                mediaDTOs[mediaIndex] = mapper.Map<PhotoDTO>(m);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        mediaIndex++;
                    }
                    var mediaGroupPostResult = bot.SendMediaGroup(tgChannel.ChannelName, telegramMedia, mediaDTOs);
                    var mediaGroupPostObject = JObject.Parse(mediaGroupPostResult);
                    if ((bool)mediaGroupPostObject["ok"])
                    {
                        foreach (var r in mediaGroupPostObject["result"])
                        {
                            string chatId = ((string)r["chat"]["id"]).Replace("-100", "");
                            string messageId = (string)r["message_id"];
                            responses.Add(new LinkPost
                            {
                                Channel = tgChannel,
                                Link = link,
                                TelegramMessageID = (int)r["message_id"],
                                PostDate = Utils.UnixTimeStampToDateTime((int)r["date"]),
                                PostLink = $"https://t.me/c/{chatId}/{messageId}",
                                PostText = (string)r["caption"]
                            });
                        }
                        numberPostedChunks++;
                    }
                    else if ((int)mediaGroupPostObject["error_code"] == 429)
                    {
                        var secondsToSleep = (int)mediaGroupPostObject["parameters"]["retry_after"];
                        Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.Provider.ProviderName} again");
                        System.Threading.Thread.Sleep((secondsToSleep + 2) * 1000);
                    }
                    else
                    {
                        Log.Error("Error from posting message to Telegram API {linkPostResult}", mediaGroupPostResult);
                        throw new Exception($"Unknown service response: {mediaGroupPostResult}");
                    }
                }
            }
            else
            {
                messageChunks = SplitMessageForTg(message);
            }

            while (numberPostedChunks < messageChunks.Count)
            {
                string chunk = messageChunks[numberPostedChunks];
                var linkPostResult = bot.SendMessage(tgChannel.ChannelName, chunk);
                var linkPostObject = JObject.Parse(linkPostResult);
                if ((bool)linkPostObject["ok"])
                {
                    string chatId = ((string)linkPostObject["result"]["chat"]["id"]).Replace("-100", "");
                    string messageId = (string)linkPostObject["result"]["message_id"];
                    responses.Add(new LinkPost
                    {
                        Channel = tgChannel,
                        Link = link,
                        TelegramMessageID = (int)linkPostObject["result"]["message_id"],
                        PostDate = Utils.UnixTimeStampToDateTime((int)linkPostObject["result"]["date"]),
                        PostLink = $"https://t.me/c/{chatId}/{messageId}",
                        PostText = (string)linkPostObject["result"]["text"]
                    });
                    numberPostedChunks++;
                }
                else if ((int)linkPostObject["error_code"] == 429)
                {
                    var secondsToSleep = (int)linkPostObject["parameters"]["retry_after"];
                    Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.Provider.ProviderName} again");
                    System.Threading.Thread.Sleep((secondsToSleep + 2) * 1000);
                }
                else
                {
                    Log.Error("Error from posting message to Telegram API {linkPostResult}", linkPostResult);
                    throw new Exception($"Unknown service response: {linkPostResult}");
                }
            }
            return responses;
        }

        public static string GetChannelInviteLink(TelegramChannel tgChannel, TelegramAPI bot)
        {
            string channelInfo = bot.GetChat(tgChannel.ChannelName);
            JObject botPostObject = JObject.Parse(channelInfo);
            while (true)
            {
                if ((bool)botPostObject["ok"])
                {
                    return (string)botPostObject["result"]["invite_link"];
                }
                else if ((int)botPostObject["error_code"] == 429)
                {
                    var secondsToSleep = (int)botPostObject["parameters"]["retry_after"];
                    Log.Verbose($"Sleeping {secondsToSleep + 2} seconds before retrying to get channel info for {tgChannel.Provider.ProviderName} again");
                    System.Threading.Thread.Sleep((secondsToSleep + 2) * 1000);
                }
                else
                {
                    return "ERROR!";
                }
                channelInfo = bot.GetChat(tgChannel.ChannelName);
                botPostObject = JObject.Parse(channelInfo);
            }
        }
    }
}