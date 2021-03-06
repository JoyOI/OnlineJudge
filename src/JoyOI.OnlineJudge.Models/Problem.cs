﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Problem.
    /// </summary>
    public class Problem
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [MaxLength(128)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [MaxLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body(markdown formatted).
        /// </summary>
        /// <value>The body.</value>
        [WebApi(FilterLevel.GetListDisabled)]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the tags, e.g. "DP:Linear", "Others:Force".
        /// </summary>
        /// <value>The tags.</value>
        [MaxLength(1024)]
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the validator code.
        /// </summary>
        /// <value>The validator code.</value>
        public string ValidatorCode { get; set; }

		/// <summary>
		/// Gets or sets the validator language.
		/// </summary>
		/// <value>The validator language.</value>
		[MaxLength(16)]
        public string ValidatorLanguage { get; set; }

        /// <summary>
        /// Gets or sets the validator BLOB identifier.
        /// </summary>
        /// <value>The validator BLOB identifier.</value>
        [WebApi(FilterLevel.GetNeedRoot)]
        public Guid? ValidatorBlobId { get; set; }

        /// <summary>
        /// Gets or sets the validator error
        /// </summary>
        /// <value>The validator error</value>
        [WebApi(FilterLevel.GetNeedOwner)]
        public string ValidatorError { get; set; }

        /// <summary>
        /// Gets or sets the standard code.
        /// </summary>
        /// <value>The standard code.</value>
        [WebApi(FilterLevel.GetNeedOwner)]
        public string StandardCode { get; set; }

		/// <summary>
		/// Gets or sets the standard language.
		/// </summary>
		/// <value>The standard language.</value>
		[MaxLength(16)]
        [WebApi(FilterLevel.GetNeedOwner)]
        public string StandardLanguage { get; set; }

        /// <summary>
        /// Gets or sets the standard program error
        /// </summary>
        /// <value>The standard program error</value>
        [WebApi(FilterLevel.GetNeedOwner)]
        public string StandardError { get; set; }

        /// <summary>
        /// Gets or sets the standard BLOB identifier.
        /// </summary>
        /// <value>The standard BLOB identifier.</value>
        [WebApi(FilterLevel.GetNeedRoot)]
        public Guid? StandardBlobId { get; set; }

        /// <summary>
        /// Gets or sets the range code.
        /// </summary>
        /// <value>The range code.</value>
        [WebApi(FilterLevel.GetNeedOwner)]
        public string RangeCode { get; set; }

        /// <summary>
        /// Gets or sets the range language.
        /// </summary>
        /// <value>The range language.</value>
        [MaxLength(16)]
        [WebApi(FilterLevel.GetNeedOwner)]
        public string RangeLanguage { get; set; }

        /// <summary>
        /// Gets or sets the range BLOB identifier.
        /// </summary>
        /// <value>The range BLOB identifier.</value>
        [WebApi(FilterLevel.GetNeedRoot)]
        public Guid? RangeBlobId { get; set; }

        /// <summary>
        /// Gets or sets the range validator error
        /// </summary>
        /// <value>The range validator error</value>
        [WebApi(FilterLevel.GetNeedOwner)]
        public string RangeError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:JoyOI.OnlineJudge.Models.Problem"/> is visiable.
        /// </summary>
        /// <value><c>true</c> if is visiable; otherwise, <c>false</c>.</value>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ProblemSource Source { get; set; }

        /// <summary>
        /// Gets or sets the difficulty.
        /// </summary>
        /// <value>The difficulty.</value>
        public int Difficulty { get; set; }

        /// <summary>
        /// Gets or sets the cached submit count.
        /// </summary>
        /// <value>The cached submit count.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public int CachedSubmitCount { get; set; }

        /// <summary>
        /// Gets or sets the cached accepted count.
        /// </summary>
        /// <value>The cached accepted count.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public int CachedAcceptedCount { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        /// <value>The created time</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the time limitation per case in ms
        /// </summary>
        /// <value>Time limitation per case in ms</value>
        [WebApi(FilterLevel.GetListDisabled)]
        public int TimeLimitationPerCaseInMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limitation per case in byte
        /// </summary>
        /// <value>Memory limitation per case in byte</value>
        [WebApi(FilterLevel.GetListDisabled)]
        public int MemoryLimitationPerCaseInByte { get; set; }

        [JsonIgnore]
        [WebApi(FilterLevel.GetListDisabled | FilterLevel.PatchDisabled)]
        public string Template { get; set; }

        [NotMapped]
        public Dictionary<string, string> CodeTemplate { get { return Template != null ? JsonConvert.DeserializeObject<Dictionary<string, string>>(Template) : null; } }

        public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
