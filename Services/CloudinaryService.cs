using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace VideoTimelineApp.Services
{
    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string SecureUrl { get; set; }
        public string PublicId { get; set; }
        public double Duration { get; set; }
        public long Bytes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class CloudinaryService
    {
        private static readonly Cloudinary _cloudinary;
        private static readonly bool _isConfigured;

        static CloudinaryService()
        {
            string cloudName = WebConfigurationManager.AppSettings["CloudinaryCloudName"];
            if (string.IsNullOrWhiteSpace(cloudName))
                cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");

            string apiKey = WebConfigurationManager.AppSettings["CloudinaryApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");

            string apiSecret = WebConfigurationManager.AppSettings["CloudinaryApiSecret"];
            if (string.IsNullOrWhiteSpace(apiSecret))
                apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

            if (!string.IsNullOrWhiteSpace(cloudName) &&
                !string.IsNullOrWhiteSpace(apiKey) &&
                !string.IsNullOrWhiteSpace(apiSecret))
            {
                var account = new Account(cloudName.Trim(), apiKey.Trim(), apiSecret.Trim());
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Timeout = 300000; // 5 minutes timeout for large video uploads
                _isConfigured = true;
            }
            else
            {
                _isConfigured = false;
            }
        }

        public static bool IsConfigured
        {
            get { return _isConfigured; }
        }

        /// <summary>
        /// Generates a secure timestamped signature for client-side direct upload from browser to Cloudinary
        /// </summary>
        public static object GetUploadSignature(string folder = "video_timeline_marker")
        {
            if (!_isConfigured) return null;

            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var parameters = new System.Collections.Generic.SortedDictionary<string, object>
            {
                { "folder", folder },
                { "timestamp", timestamp }
            };

            string signature = _cloudinary.Api.SignParameters(parameters);

            return new
            {
                cloudName = _cloudinary.Api.Account.Cloud,
                apiKey    = _cloudinary.Api.Account.ApiKey,
                timestamp = timestamp,
                signature = signature,
                folder    = folder
            };
        }

        /// <summary>
        /// Upload video from a physical or temporary file path using FileStream (best compatibility for chunked upload)
        /// </summary>
        public static async Task<CloudinaryUploadResult> UploadVideoFromFileAsync(string filePath, string originalFileName)
        {
            if (!_isConfigured)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = "Cloudinary chưa được cấu hình thông tin xác thực trong AppSettingsSecrets.config!"
                };
            }

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var uploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(originalFileName, fileStream),
                        Folder = "video_timeline_marker"
                    };

                    // UploadLargeAsync chunked upload with 10MB chunk size
                    var uploadResult = await _cloudinary.UploadLargeAsync(uploadParams, 10 * 1024 * 1024);

                    if (uploadResult.Error != null)
                    {
                        return new CloudinaryUploadResult
                        {
                            Success = false,
                            ErrorMessage = uploadResult.Error.Message
                        };
                    }

                    double dur = 0;
                    if (uploadResult.Duration > 0)
                    {
                        dur = uploadResult.Duration;
                    }

                    return new CloudinaryUploadResult
                    {
                        Success = true,
                        SecureUrl = uploadResult.SecureUrl != null ? uploadResult.SecureUrl.ToString() : null,
                        PublicId = uploadResult.PublicId,
                        Duration = dur,
                        Bytes = uploadResult.Bytes
                    };
                }
            }
            catch (Exception ex)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = BuildDetailedErrorMessage(ex)
                };
            }
        }

        /// <summary>
        /// Upload video using stream by saving to a transient temporary file first (avoids HttpInputStream disconnects)
        /// </summary>
        public static async Task<CloudinaryUploadResult> UploadVideoAsync(Stream stream, string fileName)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "cloudinary_upload_" + Guid.NewGuid().ToString("N") + Path.GetExtension(fileName));
            try
            {
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }

                return await UploadVideoFromFileAsync(tempFile, fileName);
            }
            catch (Exception ex)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = BuildDetailedErrorMessage(ex)
                };
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // Ignore transient cleanup errors
                }
            }
        }

        private static string BuildDetailedErrorMessage(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(ex.Message);

            var curr = ex.InnerException;
            int depth = 0;
            while (curr != null && depth < 5)
            {
                sb.Append(" -> ").Append(curr.Message);
                curr = curr.InnerException;
                depth++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Delete video from Cloudinary storage
        /// </summary>
        public static async Task<bool> DeleteVideoAsync(string publicId)
        {
            if (!_isConfigured || string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            try
            {
                var delParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Video
                };

                var result = await _cloudinary.DestroyAsync(delParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
