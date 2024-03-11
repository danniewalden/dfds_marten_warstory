using App;
using Domain.EventSourcing;
using Domain.Policies;
using Domain.ViewModel;
using Marten;
using Marten.Events.Projections;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MartenPresentation.Tests;

public class EventSourcingTests
{
	[Fact]
	public async Task New_Student_Using_Aggregate()
	{
		var store = await CreateStoreAndInitializeDatabase();
		var session = store.LightweightSession();

		var student = Student.Create("John Doe");
		student.EnlistInCourse(Guid.NewGuid());

		await StoreEventsFromAggregate(session, student);
	}

	[Fact]
	public async Task New_Student_Using_Only_Events()
	{
		var store = await CreateStoreAndInitializeDatabase();
		var session = store.LightweightSession();

		var studentCreated = new StudentCreated(Guid.NewGuid(), "John Doe");
		var studentEnlistedInCourse = new StudentEnlistedInCourse(studentCreated.Id, Guid.NewGuid());
		session.Events.Append(studentCreated.Id, studentCreated, studentEnlistedInCourse);
		await session.SaveChangesAsync();

		var student = await session.Events.AggregateStreamAsync<Student>(studentCreated.Id);

		student.Should().NotBeNull();
		student!.Name.Should().Be(studentCreated.Name);
		student.EnlistedCourses.Should().Contain(studentEnlistedInCourse.CourseId);
	}

	/// <summary>
	/// How to create readmodels and policies/domain event handlers using projections
	/// </summary>
	[Fact]
	public async Task Projections()
	{
		var options = Constants.DefaultOptions;

		// add viewmodel projections (inline means they are applied immediately in the same transaction as the events)
		options.Projections.Add(new CourseViewModelProjection(), ProjectionLifecycle.Inline);
		options.Projections.Add(new StudentViewmodelProjection(), ProjectionLifecycle.Inline);

		// add a policy for handling overbooked courses (async means it's applied after the transaction is committed)
		options.Projections.Add(new OverBookedCoursePolicy(), ProjectionLifecycle.Async);

		// initialize database and async projection daemon
		// allow for async projections during our unit test
		var store = await CreateStoreAndInitializeDatabase(options);
		using var daemon = await store.BuildProjectionDaemonAsync();
		await daemon.StartAllAsync();

		var session = store.LightweightSession();

		// create the math-course that allows 3 students to attend
		var mathCourseCreated = new CourseCreated(Guid.NewGuid(), "Math", 3);
		session.Events.Append(mathCourseCreated.Id, mathCourseCreated);

		// create the physics-course that allows 1 student to attend
		var physicsCourseCreated = new CourseCreated(Guid.NewGuid(), "Physics", 1);
		session.Events.Append(physicsCourseCreated.Id, physicsCourseCreated);

		// Add 4 new students, 3 of them attending the math-course and 1 attending the physics-course
		AddNewStudentAttendingCourse(session, mathCourseCreated.Id);
		AddNewStudentAttendingCourse(session, mathCourseCreated.Id);
		AddNewStudentAttendingCourse(session, physicsCourseCreated.Id);
		AddNewStudentAttendingCourse(session, mathCourseCreated.Id);

		// add a student not attending any courses
		var student5 = AddNewStudent(session);

		// add everything to the database
		await session.SaveChangesAsync();

		// take a look at the viewmodels in the database now..
		var mathCourseViewModel = await session.LoadAsync<CourseViewModel>(mathCourseCreated.Id) ?? throw new Exception("Course not found");
		mathCourseViewModel.Attendees.Should().Be(0); // this is being updated "inline" with the async policy checking for overbooked courses

		var physicsCourseViewModel = await session.LoadAsync<CourseViewModel>(physicsCourseCreated.Id) ?? throw new Exception("Course not found");
		physicsCourseViewModel.Attendees.Should().Be(0); // this is being updated "inline" with the async policy checking for overbooked courses

		// now try to enroll a student in the math-course, which is already fully booked
		var toBeMathStudent = await session.Events.AggregateStreamAsync<Student>(student5) ?? throw new Exception("Student not found");
		toBeMathStudent.EnlistInCourse(mathCourseCreated.Id); // student is fine joining the course - but this will trigger the overbooked policy

		// until the asynchronous policy has been applied, the student is still enlisted in the course
		toBeMathStudent.EnlistedCourses.Should().Contain(mathCourseCreated.Id);

		session.Events.Append(toBeMathStudent.Id, toBeMathStudent.Events.ToArray());
		await session.SaveChangesAsync();

		// now the async projection daemon should handle the overbooked policy
		await Task.Delay(10000); // wait for the async projection to finish

		var expectedMathStudentViewModel = await session.LoadAsync<StudentViewModel>(student5) ?? throw new Exception("Student not found");
		expectedMathStudentViewModel.NumberOfEnlistedCourses.Should().Be(0); // even though the student tried to enlist in the math-course, the policy should have removed the student from the course

		mathCourseViewModel = await session.LoadAsync<CourseViewModel>(mathCourseCreated.Id) ?? throw new Exception("Course not found");
		mathCourseViewModel.Attendees.Should().Be(3); // the course should still be fully booked
		
		mathCourseViewModel = await session.LoadAsync<CourseViewModel>(physicsCourseViewModel.Id) ?? throw new Exception("Course not found");
		mathCourseViewModel.Attendees.Should().Be(1); 

		await daemon.StopAllAsync();
	}

	/// <summary>
	/// When a viewmodel projection has a bug, we can re-run the projection to fix the viewmodel
	/// </summary>
	[Fact]
	public async Task RerunProjection()
	{
		// create a bug in applying the StudentEnlistedInCourseViewModel before moving on
		// verify in the database that the bug is there

		// Remember to run the "Projections" test first, as this test depends on the data generated there
		var options = Constants.DefaultOptions;

		// add viewmodel projections
		options.Projections.Add(new CourseViewModelProjection(), ProjectionLifecycle.Inline);
		options.Projections.Add(new StudentViewmodelProjection(), ProjectionLifecycle.Inline);

		// add a policy for handling overbooked courses (look, it's async)
		options.Projections.Add(new OverBookedCoursePolicy(), ProjectionLifecycle.Async);

		// initialize database and async projection daemon
		// allow for async projections during our unit test
		var store = await CreateStoreAndInitializeDatabase(options, false);
		using var daemon = await store.BuildProjectionDaemonAsync();
		await daemon.StartAllAsync();

		// fix the bug CourseViewModelProjection before re-running the projection

		// re-run the projection to get the correct viewmodel
		await daemon.RebuildProjectionAsync(typeof(CourseViewModelProjection), CancellationToken.None);

		await daemon.StopAllAsync();
	}

	#region Helpers

	private static async Task StoreEventsFromAggregate(IDocumentSession session, Student student)
	{
		session.Events.Append(student.Id, student.Events.ToArray());
		await session.SaveChangesAsync();
	}

	private static void AddNewStudentAttendingCourse(IDocumentSession session, Guid courseId)
	{
		var id = Guid.NewGuid();
		session.Events.Append(id, [new StudentCreated(id, NameGenerator.GenerateName()), new StudentEnlistedInCourse(id, courseId)]);
	}

	private static Guid AddNewStudent(IDocumentSession session)
	{
		var id = Guid.NewGuid();

		session.Events.Append(id, [new StudentCreated(id, NameGenerator.GenerateName())]);
		return id;
	}

	private static async Task<DocumentStore> CreateStoreAndInitializeDatabase(StoreOptions? options = null, bool cleanDatabase = true)
	{
		var store = new DocumentStore(options ?? Constants.DefaultOptions);

		store.Events.AddEventTypes(new[]
		{
			typeof(StudentCreated),
			typeof(StudentEnlistedInCourse),
		});

		if (cleanDatabase)
		{
			await store.Advanced.Clean.DeleteAllDocumentsAsync();
			await store.Advanced.Clean.DeleteAllEventDataAsync();
		}

		await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
		return store;
	}

	#endregion
}
