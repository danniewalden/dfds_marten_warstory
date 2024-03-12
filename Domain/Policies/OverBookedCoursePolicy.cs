using Domain.EventSourcing;
using Marten;
using Marten.Events;
using Marten.Events.Projections;

namespace Domain.Policies;

/// <summary>
/// A policy making sure no course is over booked
/// </summary>
public class OverBookedCoursePolicy : IProjection
{
	public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams) => throw new NotImplementedException();

	// This method is invoked all ALL events in the event store - manually filtering out the events we are interested in
	// this would also be a good pattern to use to do some logging, or other cross cutting concerns.
	// If you use Kafka or other message brokers, you can also use this pattern to publish events to other systems. 
	// NOTICE: Remember to handle idempotency, if you are updating data in the database or publishing events to other systems.
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
						course.EnlistStudent(enlisted.Id, enlisted.Name);
						
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
