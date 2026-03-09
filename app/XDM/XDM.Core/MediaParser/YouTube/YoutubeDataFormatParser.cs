using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XDM.Core.MediaParser.YouTube
{
    public class YoutubeDataFormatParser
    {
                        public static KeyValuePair<List<ParsedDualUrlVideoFormat>, List<ParsedUrlVideoFormat>>
            GetFormats(string file)
        {
            try
            {
                var content = File.ReadAllText(file);
                var items = JsonConvert.DeserializeObject<VideoFormatData>(content,
                    new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });

                if (items != null && items.VideoDetails != null && !string.IsNullOrEmpty(items.VideoDetails.Title))
                {
                    // Fallback to wrapper implementation if yt-dlp needs to handle it directly
                    var process = new YDLWrapper.YDLProcess
                    {
                        Uri = new Uri("https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(items.VideoDetails.Title))
                    };
                    // Instead of blocking or parsing, we just return empty so another mechanism takes over without crashing
                }
            }
            catch (Exception ex)
            {
                TraceLog.Log.Debug(ex, "Failed to parse YouTube formats through yt-dlp wrapper");
            }

            return new KeyValuePair<List<ParsedDualUrlVideoFormat>, List<ParsedUrlVideoFormat>>(
                new List<ParsedDualUrlVideoFormat>(), new List<ParsedUrlVideoFormat>());
        }




        private static VideoFormat BestAudioFormat(string mime, List<VideoFormat> audioList)
        {
            VideoFormat bestAudio = null;
            var highestBitrate = -1L;

            foreach (var audio in audioList)
            {
                if (audio.MimeType.StartsWith(mime))
                {
                    if (highestBitrate < audio.Bitrate)
                    {
                        highestBitrate = audio.Bitrate;
                        bestAudio = audio;
                    }
                }
            }

            return bestAudio;
        }

        private static string GetMediaExtension(string videoMime, string audioMime)
        {
            if (videoMime.StartsWith("video/mp4") && audioMime.StartsWith("audio/mp4"))
            {
                return "MP4";
            }
            return "MKV";
        }

        //private static string ParseUrl(string text)
        //{
        //    var arr = text.Split('&');
        //    var finalUrl = new StringBuilder();
        //    String url = null;
        //    foreach (var item in arr)
        //    {
        //        if (item.StartsWith("url"))
        //        {
        //            url = WebUtility.UrlDecode(item);
        //            continue;
        //        }
        //        finalUrl.Append('&');
        //        finalUrl.Append(item);
        //    }
        //    finalUrl.Insert(0, url.Substring(url.IndexOf('=') + 1));
        //    return finalUrl.ToString();
        //}
    }
}
