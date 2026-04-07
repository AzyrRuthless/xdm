using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XDM.Core.MediaParser.YouTube
{
    public class YoutubeDataFormatParser
    {
        public static KeyValuePair<List<ParsedDualUrlVideoFormat>, List<ParsedUrlVideoFormat>> GetFormats(string file)
        {
            var dualVideoItems = new List<ParsedDualUrlVideoFormat>();
            var videoItems = new List<ParsedUrlVideoFormat>();

            try
            {
                var content = File.ReadAllText(file);
                var items = JsonConvert.DeserializeObject<VideoFormatData>(content,
                    new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });

                if (items?.StreamingData?.AdaptiveFormats != null)
                {
                    var maxOfEachQualityVideoGroupMp4 = items.StreamingData.AdaptiveFormats
                        .Where(i => i.MimeType != null && i.MimeType.StartsWith("video/mp4") && i.Url != null)
                        .GroupBy(x => x.QualityLabel)
                        .Select(g => g.OrderByDescending(a => a.ContentLength / (a.Bitrate > 0 ? a.Bitrate : 1)).First());

                    var maxOfEachQualityVideoGroupWebm = items.StreamingData.AdaptiveFormats
                        .Where(i => i.MimeType != null && i.MimeType.StartsWith("video/webm") && i.Url != null)
                        .GroupBy(x => x.QualityLabel)
                        .Select(g => g.OrderByDescending(a => a.ContentLength / (a.Bitrate > 0 ? a.Bitrate : 1)).First());

                    var maxOfEachQualityAudioMp4 = items.StreamingData.AdaptiveFormats
                        .Where(i => i.MimeType != null && i.MimeType.StartsWith("audio/mp4") && i.Url != null)
                        .GroupBy(x => x.QualityLabel + x.MimeType)
                        .Select(g => g.OrderByDescending(a => a.ContentLength / (a.Bitrate > 0 ? a.Bitrate : 1)).First());

                    var maxOfEachQualityAudioWebm = items.StreamingData.AdaptiveFormats
                       .Where(i => i.MimeType != null && i.MimeType.StartsWith("audio/webm") && i.Url != null)
                       .GroupBy(x => x.QualityLabel + x.MimeType)
                       .Select(g => g.OrderByDescending(a => a.ContentLength / (a.Bitrate > 0 ? a.Bitrate : 1)).First());

                    if (maxOfEachQualityVideoGroupMp4 != null && maxOfEachQualityAudioMp4 != null)
                    {
                        foreach (var video in maxOfEachQualityVideoGroupMp4)
                        {
                            foreach (var audio in maxOfEachQualityAudioMp4)
                            {
                                var ext = GetMediaExtension(video.MimeType, audio.MimeType);
                                dualVideoItems.Add(
                                    new ParsedDualUrlVideoFormat(items.VideoDetails?.Title ?? "YouTube Video",
                                        video.Url!,
                                        audio.Url!,
                                        video.QualityLabel ?? "Unknown Quality",
                                        ext,
                                        video.ContentLength + audio.ContentLength
                                    )
                                );
                            }
                        }
                    }

                    if (maxOfEachQualityVideoGroupWebm != null && maxOfEachQualityAudioWebm != null)
                    {
                        foreach (var video in maxOfEachQualityVideoGroupWebm)
                        {
                            foreach (var audio in maxOfEachQualityAudioWebm)
                            {
                                var ext = GetMediaExtension(video.MimeType, audio.MimeType);
                                dualVideoItems.Add(
                                    new ParsedDualUrlVideoFormat(items.VideoDetails?.Title ?? "YouTube Video",
                                        video.Url!,
                                        audio.Url!,
                                        video.QualityLabel ?? "Unknown Quality",
                                        ext,
                                        video.ContentLength + audio.ContentLength
                                    )
                                );
                            }
                        }
                    }
                }

                if (items?.StreamingData?.Formats != null)
                {
                    videoItems.AddRange(
                        items.StreamingData.Formats.Where(
                            item => item.MimeType != null && item.MimeType.StartsWith("video/") && item.Url != null).Select(
                                item => new ParsedUrlVideoFormat(items.VideoDetails?.Title ?? "YouTube Video",
                                    item.Url!,
                                    item.QualityLabel ?? "Unknown Quality",
                                    (item.MimeType!.StartsWith("video/mp4") ? "MP4" : "MKV"),
                                    item.ContentLength)));
                }
            }
            catch (Exception ex)
            {
                TraceLog.Log.Debug(ex, "Failed to parse YouTube formats");
            }

            return new KeyValuePair<List<ParsedDualUrlVideoFormat>, List<ParsedUrlVideoFormat>>(dualVideoItems, videoItems);
        }

        private static string GetMediaExtension(string? videoMime, string? audioMime)
        {
            if (videoMime != null && audioMime != null &&
                videoMime.StartsWith("video/mp4") && audioMime.StartsWith("audio/mp4"))
            {
                return "MP4";
            }
            return "MKV";
        }
    }
}
