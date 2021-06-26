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
            if (isFirst && isLast)
            {
                return message;
            }
            else if (isFirst && !isLast)
            {
                return message + "[...]";
            }
            else if (!isFirst && !isLast)
            {
                return "[...]" + message + "[...]";
            }
            else
            {
                return "[...]" + message;
            }
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
                var closestBreak = reverseChunk.IndexOf("\n", 0, (int)reverseChunk.Length / 3);
                //if no new line found trying to break by dot  in the last third of block
                var closestDot = reverseChunk.IndexOf(".", 0, (int)reverseChunk.Length / 3);
                //if no dot found trying to break by space
                var closestSpace = reverseChunk.IndexOf(" ", 0, (int)reverseChunk.Length / 3);

                if (closestBreak >= 0)
                {
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit - closestBreak), true, false));
                    splitPosition = tgCaptionSizeLimit - closestBreak;
                }else if (closestDot >= 0)
                {
                    messageChunks.Add(DecorateTelegramString(message.Substring(0, tgCaptionSizeLimit - closestDot), true, false));
                    splitPosition = tgCaptionSizeLimit - closestDot;
                }else if (closestSpace >= 0)
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
                        var closestBreak = reverseChunk.IndexOf("\n", 0, (int)reverseChunk.Length / 3);
                        if (closestBreak >= 0)
                        {
                            messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit - closestBreak), i == 0, false));
                            i += telegramMessageLimit - closestBreak;
                            continue;
                        }
                        //if no new line found trying to break by dot
                        var closestDot = reverseChunk.IndexOf(".", 0, (int)reverseChunk.Length / 3);
                        if (closestDot >= 0)
                        {
                            messageChunks.Add(DecorateTelegramString(message.Substring(i, telegramMessageLimit - closestDot), i == 0, false));
                            i += telegramMessageLimit - closestDot;
                            continue;
                        }
                        //if no dot found trying to break by space
                        var closestSpace = reverseChunk.IndexOf(" ", 0, (int)reverseChunk.Length / 3);
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
        private static string EscapeTgString(string originalString)
        {
            StringBuilder sb = new(originalString);
            sb.Replace("&Agrave;", "&#192;");
            sb.Replace("&Aacute;", "&#193;");
            sb.Replace("&Acirc;", "&#194;");
            sb.Replace("&Atilde;", "&#195;");
            sb.Replace("&Auml;", "&#196;");
            sb.Replace("&Aring;", "&#197;");
            sb.Replace("&AElig;", "&#198;");
            sb.Replace("&Ccedil;", "&#199;");
            sb.Replace("&Egrave;", "&#200;");
            sb.Replace("&Eacute;", "&#201;");
            sb.Replace("&Ecirc;", "&#202;");
            sb.Replace("&Euml;", "&#203;");
            sb.Replace("&Igrave;", "&#204;");
            sb.Replace("&Iacute;", "&#205;");
            sb.Replace("&Icirc;", "&#206;");
            sb.Replace("&Iuml;", "&#207;");
            sb.Replace("&ETH;", "&#208;");
            sb.Replace("&Ntilde;", "&#209;");
            sb.Replace("&Ograve;", "&#210;");
            sb.Replace("&Oacute;", "&#211;");
            sb.Replace("&Ocirc;", "&#212;");
            sb.Replace("&Otilde;", "&#213;");
            sb.Replace("&Ouml;", "&#214;");
            sb.Replace("&Oslash;", "&#216;");
            sb.Replace("&Ugrave;", "&#217;");
            sb.Replace("&Uacute;", "&#218;");
            sb.Replace("&Ucirc;", "&#219;");
            sb.Replace("&Uuml;", "&#220;");
            sb.Replace("&Yacute;", "&#221;");
            sb.Replace("&THORN;", "&#222;");
            sb.Replace("&szlig;", "&#223;");
            sb.Replace("&agrave;", "&#224;");
            sb.Replace("&aacute;", "&#225;");
            sb.Replace("&acirc;", "&#226;");
            sb.Replace("&atilde;", "&#227;");
            sb.Replace("&auml;", "&#228;");
            sb.Replace("&aring;", "&#229;");
            sb.Replace("&aelig;", "&#230;");
            sb.Replace("&ccedil;", "&#231;");
            sb.Replace("&egrave;", "&#232;");
            sb.Replace("&eacute;", "&#233;");
            sb.Replace("&ecirc;", "&#234;");
            sb.Replace("&euml;", "&#235;");
            sb.Replace("&igrave;", "&#236;");
            sb.Replace("&iacute;", "&#237;");
            sb.Replace("&icirc;", "&#238;");
            sb.Replace("&iuml;", "&#239;");
            sb.Replace("&eth;", "&#240;");
            sb.Replace("&ntilde;", "&#241;");
            sb.Replace("&ograve;", "&#242;");
            sb.Replace("&oacute;", "&#243;");
            sb.Replace("&ocirc;", "&#244;");
            sb.Replace("&otilde;", "&#245;");
            sb.Replace("&ouml;", "&#246;");
            sb.Replace("&oslash;", "&#248;");
            sb.Replace("&ugrave;", "&#249;");
            sb.Replace("&uacute;", "&#250;");
            sb.Replace("&ucirc;", "&#251;");
            sb.Replace("&uuml;", "&#252;");
            sb.Replace("&yacute;", "&#253;");
            sb.Replace("&thorn;", "&#254;");
            sb.Replace("&yuml;", "&#255;");
            sb.Replace("&nbsp;", "&#160;");
            sb.Replace("&iexcl;", "&#161;");
            sb.Replace("&cent;", "&#162;");
            sb.Replace("&pound;", "&#163;");
            sb.Replace("&curren;", "&#164;");
            sb.Replace("&yen;", "&#165;");
            sb.Replace("&brvbar;", "&#166;");
            sb.Replace("&sect;", "&#167;");
            sb.Replace("&uml;", "&#168;");
            sb.Replace("&copy;", "&#169;");
            sb.Replace("&ordf;", "&#170;");
            sb.Replace("&laquo;", "&#171;");
            sb.Replace("&not;", "&#172;");
            sb.Replace("&shy;", "&#173;");
            sb.Replace("&reg;", "&#174;");
            sb.Replace("&macr;", "&#175;");
            sb.Replace("&deg;", "&#176;");
            sb.Replace("&plusmn;", "&#177;");
            sb.Replace("&sup2;", "&#178;");
            sb.Replace("&sup3;", "&#179;");
            sb.Replace("&acute;", "&#180;");
            sb.Replace("&micro;", "&#181;");
            sb.Replace("&para;", "&#182;");
            sb.Replace("&cedil;", "&#184;");
            sb.Replace("&sup1;", "&#185;");
            sb.Replace("&ordm;", "&#186;");
            sb.Replace("&raquo;", "&#187;");
            sb.Replace("&frac14;", "&#188;");
            sb.Replace("&frac12;", "&#189;");
            sb.Replace("&frac34;", "&#190;");
            sb.Replace("&iquest;", "&#191;");
            sb.Replace("&times;", "&#215;");
            sb.Replace("&divide;", "&#247;");
            sb.Replace("&forall;", "&#8704;");
            sb.Replace("&part;", "&#8706;");
            sb.Replace("&exist;", "&#8707;");
            sb.Replace("&empty;", "&#8709;");
            sb.Replace("&nabla;", "&#8711;");
            sb.Replace("&isin;", "&#8712;");
            sb.Replace("&notin;", "&#8713;");
            sb.Replace("&ni;", "&#8715;");
            sb.Replace("&prod;", "&#8719;");
            sb.Replace("&sum;", "&#8721;");
            sb.Replace("&minus;", "&#8722;");
            sb.Replace("&lowast;", "&#8727;");
            sb.Replace("&radic;", "&#8730;");
            sb.Replace("&prop;", "&#8733;");
            sb.Replace("&infin;", "&#8734;");
            sb.Replace("&ang;", "&#8736;");
            sb.Replace("&and;", "&#8743;");
            sb.Replace("&or;", "&#8744;");
            sb.Replace("&cap;", "&#8745;");
            sb.Replace("&cup;", "&#8746;");
            sb.Replace("&int;", "&#8747;");
            sb.Replace("&there4;", "&#8756;");
            sb.Replace("&sim;", "&#8764;");
            sb.Replace("&cong;", "&#8773;");
            sb.Replace("&asymp;", "&#8776;");
            sb.Replace("&ne;", "&#8800;");
            sb.Replace("&equiv;", "&#8801;");
            sb.Replace("&le;", "&#8804;");
            sb.Replace("&ge;", "&#8805;");
            sb.Replace("&sub;", "&#8834;");
            sb.Replace("&sup;", "&#8835;");
            sb.Replace("&nsub;", "&#8836;");
            sb.Replace("&sube;", "&#8838;");
            sb.Replace("&supe;", "&#8839;");
            sb.Replace("&oplus;", "&#8853;");
            sb.Replace("&otimes;", "&#8855;");
            sb.Replace("&perp;", "&#8869;");
            sb.Replace("&sdot;", "&#8901;");
            sb.Replace("&Alpha;", "&#913;");
            sb.Replace("&Beta;", "&#914;");
            sb.Replace("&Gamma;", "&#915;");
            sb.Replace("&Delta;", "&#916;");
            sb.Replace("&Epsilon;", "&#917;");
            sb.Replace("&Zeta;", "&#918;");
            sb.Replace("&Eta;", "&#919;");
            sb.Replace("&Theta;", "&#920;");
            sb.Replace("&Iota;", "&#921;");
            sb.Replace("&Kappa;", "&#922;");
            sb.Replace("&Lambda;", "&#923;");
            sb.Replace("&Mu;", "&#924;");
            sb.Replace("&Nu;", "&#925;");
            sb.Replace("&Xi;", "&#926;");
            sb.Replace("&Omicron;", "&#927;");
            sb.Replace("&Pi;", "&#928;");
            sb.Replace("&Rho;", "&#929;");
            sb.Replace("&Sigma;", "&#931;");
            sb.Replace("&Tau;", "&#932;");
            sb.Replace("&Upsilon;", "&#933;");
            sb.Replace("&Phi;", "&#934;");
            sb.Replace("&Chi;", "&#935;");
            sb.Replace("&Psi;", "&#936;");
            sb.Replace("&Omega;", "&#937;");
            sb.Replace("&alpha;", "&#945;");
            sb.Replace("&beta;", "&#946;");
            sb.Replace("&gamma;", "&#947;");
            sb.Replace("&delta;", "&#948;");
            sb.Replace("&epsilon;", "&#949;");
            sb.Replace("&zeta;", "&#950;");
            sb.Replace("&eta;", "&#951;");
            sb.Replace("&theta;", "&#952;");
            sb.Replace("&iota;", "&#953;");
            sb.Replace("&kappa;", "&#954;");
            sb.Replace("&lambda;", "&#955;");
            sb.Replace("&mu;", "&#956;");
            sb.Replace("&nu;", "&#957;");
            sb.Replace("&xi;", "&#958;");
            sb.Replace("&omicron;", "&#959;");
            sb.Replace("&pi;", "&#960;");
            sb.Replace("&rho;", "&#961;");
            sb.Replace("&sigmaf;", "&#962;");
            sb.Replace("&sigma;", "&#963;");
            sb.Replace("&tau;", "&#964;");
            sb.Replace("&upsilon;", "&#965;");
            sb.Replace("&phi;", "&#966;");
            sb.Replace("&chi;", "&#967;");
            sb.Replace("&psi;", "&#968;");
            sb.Replace("&omega;", "&#969;");
            sb.Replace("&thetasym;", "&#977;");
            sb.Replace("&upsih;", "&#978;");
            sb.Replace("&piv;", "&#982;");
            sb.Replace("&OElig;", "&#338;");
            sb.Replace("&oelig;", "&#339;");
            sb.Replace("&Scaron;", "&#352;");
            sb.Replace("&scaron;", "&#353;");
            sb.Replace("&Yuml;", "&#376;");
            sb.Replace("&fnof;", "&#402;");
            sb.Replace("&circ;", "&#710;");
            sb.Replace("&tilde;", "&#732;");
            sb.Replace("&ensp;", "&#8194;");
            sb.Replace("&emsp;", "&#8195;");
            sb.Replace("&thinsp;", "&#8201;");
            sb.Replace("&zwnj;", "&#8204;");
            sb.Replace("&zwj;", "&#8205;");
            sb.Replace("&lrm;", "&#8206;");
            sb.Replace("&rlm;", "&#8207;");
            sb.Replace("&ndash;", "&#8211;");
            sb.Replace("&mdash;", "&#8212;");
            sb.Replace("&lsquo;", "&#8216;");
            sb.Replace("&rsquo;", "&#8217;");
            sb.Replace("&sbquo;", "&#8218;");
            sb.Replace("&ldquo;", "&#8220;");
            sb.Replace("&rdquo;", "&#8221;");
            sb.Replace("&bdquo;", "&#8222;");
            sb.Replace("&dagger;", "&#8224;");
            sb.Replace("&Dagger;", "&#8225;");
            sb.Replace("&bull;", "&#8226;");
            sb.Replace("&hellip;", "&#8230;");
            sb.Replace("&permil;", "&#8240;");
            sb.Replace("&prime;", "&#8242;");
            sb.Replace("&Prime;", "&#8243;");
            sb.Replace("&lsaquo;", "&#8249;");
            sb.Replace("&rsaquo;", "&#8250;");
            sb.Replace("&oline;", "&#8254;");
            sb.Replace("&euro;", "&#8364;");
            sb.Replace("&trade;", "&#8482;");
            sb.Replace("&larr;", "&#8592;");
            sb.Replace("&uarr;", "&#8593;");
            sb.Replace("&rarr;", "&#8594;");
            sb.Replace("&darr;", "&#8595;");
            sb.Replace("&harr;", "&#8596;");
            sb.Replace("&crarr;", "&#8629;");
            sb.Replace("&lceil;", "&#8968;");
            sb.Replace("&rceil;", "&#8969;");
            sb.Replace("&lfloor;", "&#8970;");
            sb.Replace("&rfloor;", "&#8971;");
            sb.Replace("&loz;", "&#9674;");
            sb.Replace("&spades;", "&#9824;");
            sb.Replace("&clubs;", "&#9827;");
            sb.Replace("&hearts;", "&#9829;");
            sb.Replace("&diams;", "&#9830;");

            return sb.ToString();
        }
        public static List<DigestPost> AddDigestPost(TelegramChannel tgChannel, Digest digest, TelegramAPI bot, IServiceProvider serviceProvider)
        {
            //var parser = ParserFactory.Setup(tgChannel.Provider, serviceProvider);
            IEnumerable<IParser> serviceCollection = serviceProvider.GetServices<IParser>();
            var parser = serviceCollection.FirstOrDefault(p => p.CurrentProvider == tgChannel.Provider.ProviderName);
            string message = EscapeTgString(parser.FormatDigestPost(digest));
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
                    Log.Information($"Sleeping {secondsToSleep + 2} seconds before retrying to send to {tgChannel.ChannelName} again");
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
            string message = EscapeTgString(parser.FormatLinkPost(link));
            List<string> messageChunks = new();
            List<LinkPost> responses = new();
            int numberPostedChunks = 0;
            if (link.Medias != null && link.Medias.Any())
            {
                messageChunks = SplitCaptionForTg(message);
                while(numberPostedChunks < 1)
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
                    foreach(var m in mediasToPost)
                    {
                        string currentCaption;
                        if (mediaIndex == 0) {
                            currentCaption = chunk;
                        }
                        else
                        {
                            currentCaption = "";
                        }
                        switch (m.GetType().Name) {
                            case "Photo":
                                telegramMedia[mediaIndex] = new TelegramPhoto {
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
                    //var linkPostResult = bot.SendMessage(tgChannel.ChannelName, chunk);
                    var mediaGroupPostObject = JObject.Parse(mediaGroupPostResult);
                    if ((bool)mediaGroupPostObject["ok"])
                    {
                        foreach(var r in mediaGroupPostObject["result"])
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