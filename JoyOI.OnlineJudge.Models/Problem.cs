using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class Problem
    {
        [MaxLength(128)]
        public string Id { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        public string Body { get; set; }

        public string Tags { get; set; }

        public string ValidatorCode { get; set; }

        public string ValidatorLanguage { get; set; }

        public Guid? ValidatorBlobId { get; set; }

        public string StandardCode { get; set; }

        public string StandardLanguage { get; set; }

        public Guid? StandardBlobId { get; set; }

        public string RangeCode { get; set; }

        public string RangeLanguage { get; set; }

        public Guid? RangeBlobId { get; set; }

        public bool IsVisiable { get; set; }

        [MaxLength(64)]
        public string Source { get; set; }
    }
}
