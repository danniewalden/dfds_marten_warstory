namespace Domain.EventSourcing;

public class Course : AggregateRoot
{
	public Guid Id { get; private set; }
	public uint MaxAttendees { get; private set; }
	public uint CurrentAttendees { get; private set; }
	
	// The command for enlisting a student in a course is responsible for enforcing the business rule that a course cannot be overbooked.
	// If needed, it can also apply the state directly, using the Apply method - but the important thing here is, the events are raised.
	public void EnlistStudent(Guid enlistedStudentId, string enlistedStudentName)
	{
		if (MaxAttendees <= CurrentAttendees)
		{
			Raise(new CourseOverbooked(Id, enlistedStudentId));
			return;
		}

		var studentEnlistedInCourse = new StudentEnlisted(enlistedStudentId, Id, enlistedStudentName);
		Raise(studentEnlistedInCourse);
		Apply(studentEnlistedInCourse);
	}

	// invoked by marten when applying the events from the event store 
	// the only thing apply methods does, is applying the state changes from the events.
	// the decision making is done in the command methods, so we can safely apply the events without doing any checks.
	private void Apply(CourseCreated courseCreated)
	{
		// even though the event contains the name of the course, we dont need it for our enforced business rules.
		// no need to store it in the aggregate state. (one of the nice things about event sourcing) - this can always be added later, as all the events are still in the event store
		// projections can still use the name, if needed.
		MaxAttendees = courseCreated.MaxAttendees;
	}

	// invoked by marten when applying the events from the event store 
	private void Apply(StudentEnlisted _)
	{
		CurrentAttendees++;
	}
}

public record CourseCreated(Guid Id, string Name, uint MaxAttendees);

public record CourseOverbooked(Guid Id, Guid StudentId);

public record StudentEnlisted(Guid Id, Guid StudentId, string StudentName);
