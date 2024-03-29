namespace Domain.EventSourcing;

public class Student : AggregateRoot
{
	public Guid Id { get; private set; }
	public string Name { get; private set; } = null!;
	public List<Guid> EnlistedCourses { get; private set; } = new();

	public void EnlistInCourse(Guid courseId)
	{
		if (EnlistedCourses.Contains(courseId)) return;

		var studentEnlistedInCourse = new StudentEnlistedInCourse(Id, Name, courseId);
		Raise(studentEnlistedInCourse);
		Apply(studentEnlistedInCourse);
		EnlistedCourses.Add(courseId);
	}

	public static Student Create(string name)
	{
		var student = new Student();
		var studentCreated = new StudentCreated(Guid.NewGuid(), name);
		student.Raise(studentCreated);
		student.Apply(studentCreated);
		return student;
	}

	public void DelistFromCourse(Guid courseId)
	{
		if (!EnlistedCourses.Contains(courseId)) return;

		var studentDelistedFromCourse = new StudentDelistedFromCourse(Id, courseId);
		Raise(studentDelistedFromCourse);
		Apply(studentDelistedFromCourse);
	}

	private void Apply(StudentEnlistedInCourse studentEnlistedInCourse)
	{
		EnlistedCourses.Add(studentEnlistedInCourse.CourseId);
	}

	private void Apply(StudentCreated studentCreated)
	{
		Name = studentCreated.Name;
		Id = studentCreated.Id;
	}

	private void Apply(StudentDelistedFromCourse studentDelistedFromCourse)
	{
		EnlistedCourses.Remove(studentDelistedFromCourse.CourseId);
	}
}

public record StudentDelistedFromCourse(Guid Id, Guid CourseId);

public record StudentCreated(Guid Id, string Name);

public record StudentEnlistedInCourse(Guid Id, string Name, Guid CourseId);
