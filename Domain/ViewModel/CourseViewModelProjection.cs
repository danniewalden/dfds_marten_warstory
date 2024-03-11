using Domain.EventSourcing;
using Marten.Events.Aggregation;

namespace Domain.ViewModel;

public class CourseViewModelProjection : SingleStreamProjection<CourseViewModel>
{
	public static CourseViewModel Create(CourseCreated courseCreated) => new(courseCreated.Id, courseCreated.Name, courseCreated.MaxAttendees, 0);

	public CourseViewModel Apply(CourseViewModel model, StudentAddedToCourse _) => model with { Attendees = model.Attendees + 1 };
}

public record CourseViewModel(Guid Id, string Name, uint MaxAttendees, uint Attendees);
