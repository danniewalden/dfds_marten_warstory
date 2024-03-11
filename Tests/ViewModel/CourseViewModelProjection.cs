using Domain.EventSourcing;
using Marten.Events.Projections;

namespace MartenPresentation.Tests.ViewModel;

public class CourseViewModelProjection : MultiStreamProjection<CourseViewModel, Guid>
{
	public CourseViewModelProjection()
	{
		Identity<CourseCreated>(p => p.Id);
		Identity<StudentEnlistedInCourse>(p => p.CourseId);
		Identity<StudentDelistedFromCourse>(p => p.CourseId);
	}

	public static CourseViewModel Create(CourseCreated courseCreated) => new(courseCreated.Id, courseCreated.Name, courseCreated.MaxAttendees, 0);

	public CourseViewModel Apply(CourseViewModel model, StudentEnlistedInCourse _) => model with { Attendees = model.Attendees + 1 };
	public CourseViewModel Apply(CourseViewModel model, StudentDelistedFromCourse _) => model with { Attendees = model.Attendees - 1 };
}

public record CourseViewModel(Guid Id, string Name, uint MaxAttendees, uint Attendees);