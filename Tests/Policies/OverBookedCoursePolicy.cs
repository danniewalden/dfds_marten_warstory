using Domain.EventSourcing;
using Marten;
using Marten.Events;
using Marten.Events.Projections;

namespace MartenPresentation.Tests.Policies;

public class OverBookedCoursePolicy : IProjection
{
	public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams) => throw new NotImplementedException();

	public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams, CancellationToken cancellation)
	{
		foreach (var stream in streams)	
		{
			foreach (var @event in stream.Events)
			{
				switch (@event.Data)
				{
					case StudentEnlistedInCourse enlisted:
					{
						var session = operations.DocumentStore.LightweightSession();
						var course = await session.Events.AggregateStreamAsync<Course>(enlisted.CourseId, token: cancellation) ?? throw new Exception("Course not found");
						course.EnrollStudent(enlisted.Id);
						
						session.Events.Append(course.Id, course.Events.ToArray());
						await session.SaveChangesAsync(cancellation);
						break;
					}
					case CourseOverbooked overbooked:
					{
						var session = operations.DocumentStore.LightweightSession();
						var student = await session.Events.AggregateStreamAsync<Student>(overbooked.StudentId, token: cancellation) ?? throw new Exception("Student not found");
						student.DelistFromCourse(overbooked.Id);
						
						session.Events.Append(student.Id, student.Events.ToArray());
						await session.SaveChangesAsync(cancellation);
						break;
					}
				}
			}
		}
	}
}
