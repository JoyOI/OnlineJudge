using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Online judge context.
    /// </summary>
    public class OnlineJudgeContext : IdentityDbContext<User, Role, Guid>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:JoyOI.OnlineJudge.Models.OnlineJudgeContext"/> class.
        /// </summary>
        /// <param name="opt">Db context option.</param>
        public OnlineJudgeContext(DbContextOptions opt) 
            : base(opt)
        {
        }

        public Task InitializeAsync()
        {
            return Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Gets or sets the attendees.
        /// </summary>
        /// <value>The attendees.</value>
        public DbSet<Attendee> Attendees { get; set; }

        /// <summary>
        /// Gets or sets the configurations
        /// </summary>
        /// <value>The configurations</value>
        public DbSet<Configuration> Configurations { get; set; }

        /// <summary>
        /// Gets or sets the contests.
        /// </summary>
        /// <value>The contests.</value>
        public DbSet<Contest> Contests { get; set; }

        /// <summary>
        /// Gets or sets the contest problems.
        /// </summary>
        /// <value>The contest problems.</value>
        public DbSet<ContestProblem> ContestProblems { get; set; }

        /// <summary>
        /// Gets or sets the contest problem last statuses.
        /// </summary>
        /// <value>The contest problem last statuses.</value>
        public DbSet<ContestProblemLastStatus> ContestProblemLastStatuses { get; set; }

        /// <summary>
        /// Gets or sets the groups.
        /// </summary>
        /// <value>The groups.</value>
        public DbSet<Group> Groups { get; set; }

        /// <summary>
        /// Gets or sets the group join requests.
        /// </summary>
        /// <value>The group join requests.</value>
        public DbSet<GroupMember> GroupMembers { get; set; }

        /// <summary>
        /// Gets or sets the hack statuses.
        /// </summary>
        /// <value>The hack statuses.</value>
        public DbSet<HackStatus> HackStatuses { get; set; }

        /// <summary>
        /// Gets or sets the hack status state machines.
        /// </summary>
        /// <value>The hack status state machines.</value>
        public DbSet<HackStatusStateMachine> HackStatusStateMachines { get; set; }

        /// <summary>
        /// Gets or sets the judge statuses.
        /// </summary>
        /// <value>The judge statuses.</value>
        public DbSet<JudgeStatus> JudgeStatuses { get; set; }

        /// <summary>
        /// Gets or sets the judge status state machines.
        /// </summary>
        /// <value>The judge status state machines.</value>
        public DbSet<JudgeStatusStateMachine> JudgeStatusStateMachines { get; set; }

        /// <summary>
        /// Gets or sets the problems.
        /// </summary>
        /// <value>The problems.</value>
        public DbSet<Problem> Problems { get; set; }

        /// <summary>
        /// Gets or sets the protected problem identifier prefixes.
        /// </summary>
        /// <value>The protected problem identifier prefixes.</value>
        public DbSet<ProtectedProblemIdPrefix> ProtectedProblemIdPrefixes { get; set; }

        /// <summary>
        /// Gets or sets the shared validators
        /// </summary>
        /// <value>The shared validators</value>
        public DbSet<SharedValidator> SharedValidators { get; set; }

        /// <summary>
        /// Gets or sets the state machines.
        /// </summary>
        /// <value>The state machines.</value>
        public DbSet<StateMachine> StateMachines { get; set; }

        /// <summary>
        /// Gets or sets the sub judge statuses.
        /// </summary>
        /// <value>The sub judge statuses.</value>
        public DbSet<SubJudgeStatus> SubJudgeStatuses { get; set; }

        /// <summary>
        /// Gets or sets the test cases.
        /// </summary>
        /// <value>The test cases.</value>
        public DbSet<TestCase> TestCases { get; set; }

        /// <summary>
        /// Gets or sets the test case buy logs
        /// </summary>
        /// <value>The test cases buy logs.</value>
        public DbSet<TestCasePurchase> TestCasePurchases { get; set; }

        /// <summary>
        /// Gets or sets the virtual judge users.
        /// </summary>
        /// <value>The virtual judge users.</value>
        public DbSet<VirtualJudgeUser> VirtualJudgeUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Attendee>(e =>
			{
                e.HasKey(x => new { x.ContestId, x.UserId });
				e.HasIndex(x => x.IsVirtual);
            });

            builder.Entity<Clarification>(e =>
            {
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CreatedTime);
            });

            builder.Entity<Contest>(e =>
			{
                e.HasIndex(x => x.AttendPermission);
				e.HasIndex(x => x.Begin);
				e.HasIndex(x => x.Duration);
				e.HasIndex(x => x.Domain);
                e.HasIndex(x => x.IsHighlighted);
				e.HasIndex(x => x.Type);
                e.HasIndex(x => x.Title).ForMySqlIsFullText();
                e.HasMany(x => x.JudgeStatuses).WithOne(x => x.Contest).IsRequired(false);
            });

            builder.Entity<ContestProblem>(e =>
			{
                e.HasKey(x => new { x.ContestId, x.ProblemId });
				e.HasIndex(x => x.Number);
                e.HasIndex(x => x.Point);
            });

            builder.Entity<ContestProblemLastStatus>(e =>
			{
                e.HasKey(x => new { x.ContestId, x.ProblemId, x.UserId, x.StatusId });
				e.HasIndex(x => x.Point);
				e.HasIndex(x => x.Point2);
				e.HasIndex(x => x.Point3);
				e.HasIndex(x => x.TimeSpan);
				e.HasIndex(x => x.TimeSpan2);
                e.HasIndex(x => x.IsLocked);
            });

            builder.Entity<Group>(e =>
			{
				e.HasIndex(x => x.CreatedTime);
				e.HasIndex(x => x.Domain);
				e.HasIndex(x => x.Level);
				e.HasIndex(x => x.Point);
				e.HasIndex(x => x.Name).ForMySqlIsFullText();
                e.HasIndex(x => x.Type);
            });

            builder.Entity<GroupMember>(e =>
            {
                e.HasKey(x => new { x.GroupId, x.UserId });
				e.HasIndex(x => x.CreatedTime);
				e.HasIndex(x => x.Status);
            });

            builder.Entity<HackStatus>(e =>
			{
				e.HasIndex(x => x.Time);
				e.HasIndex(x => x.Result);
			});

            builder.Entity<HackStatusStateMachine>(e =>
			{
                e.HasKey(x => new { x.StateMachineId, x.StatusId });
			});

            builder.Entity<JudgeStatus>(e =>
			{
				e.HasIndex(x => x.CreatedTime);
				e.HasIndex(x => x.Result);
                e.HasIndex(x => x.Language);
			});

            builder.Entity<JudgeStatusStateMachine>(e =>
            {
                e.HasKey(x => new { x.StateMachineId, x.StatusId });
            });

            builder.Entity<Problem>(e =>
            {
                e.HasIndex(x => x.CreatedTime);
                e.HasIndex(x => x.Difficulty);
                e.HasIndex(x => x.Source);
                e.HasIndex(x => x.Tags).ForMySqlIsFullText();
                e.HasIndex(x => x.Title).ForMySqlIsFullText();
            });

            builder.Entity<ProtectedProblemIdPrefix>(e =>
            {
                e.HasKey(x => x.Value);
            });

            builder.Entity<SharedValidator>(e =>
            {
                e.HasIndex(x => x.IsDefault);
            });

            builder.Entity<SubJudgeStatus>(e =>
			{
                e.HasKey(x => new { x.StatusId, x.SubId });
				e.HasIndex(x => x.Result);
            });

            builder.Entity<TestCase>(e =>
            {
                e.HasIndex(x => x.Type);
			});

            builder.Entity<TestCasePurchase>(e =>
            {
                e.HasKey(x => new { x.UserId, x.TestCaseId });
            });

            builder.Entity<User>(e =>
			{
				e.HasIndex(x => x.OpenId);
				e.HasIndex(x => x.Nickname);
			});

            builder.Entity<VirtualJudgeUser>(e =>
			{
				e.HasIndex(x => x.Source);
                e.HasIndex(x => x.IsInUse);
            });
        }
    }
}
