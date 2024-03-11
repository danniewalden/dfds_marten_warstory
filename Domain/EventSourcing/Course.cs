namespace Domain.EventSourcing;

public class Course : AggregateRoot
{
	public Guid Id { get; private set; }
	public string Name { get; private set; }
	public uint MaxAttendees { get; private set; }
	public uint CurrentAttendees { get; private set; }

	private Course()
	{
		Name = null!;
	}

	public static Course New(string name, uint maxAttendees)
	{
		var course = new Course();
		var courseCreated = new CourseCreated(Guid.NewGuid(), name, maxAttendees);
		course.Raise(courseCreated);
		course.Apply(courseCreated);
		return course;
	}

	private void Apply(CourseCreated courseCreated)
	{
		Name = courseCreated.Name;
		MaxAttendees = courseCreated.MaxAttendees;
	}

	public void EnrollStudent(Guid enlistedStudentId)
	{
		if (MaxAttendees <= CurrentAttendees)
		{
			Raise(new CourseOverbooked(Id, enlistedStudentId));
			return;
		}

		var studentEnlistedInCourse = new StudentAddedToCourse(enlistedStudentId, Id);
		Raise(studentEnlistedInCourse);
		Apply(studentEnlistedInCourse);
	}

	private void Apply(StudentAddedToCourse _)
	{
		CurrentAttendees++;
	}
}

public record CourseCreated(Guid Id, string Name, uint MaxAttendees);
public record CourseOverbooked(Guid Id, Guid StudentId);
public record StudentAddedToCourse(Guid Id, Guid StudentId);
