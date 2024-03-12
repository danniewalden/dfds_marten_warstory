using Domain.EventSourcing;
using Marten.Events.Aggregation;

namespace Domain.ViewModel;

/// <summary>
/// A single stream projection enables the creation of a view model from a single stream of events. (in this case, the Course stream)
/// </summary>
public class CourseViewModelProjection : SingleStreamProjection<CourseViewModel>
{
	public static CourseViewModel Create(CourseCreated courseCreated) => new(courseCreated.Id, courseCreated.Name, 0, Array.Empty<string>());

	public CourseViewModel Apply(CourseViewModel model, StudentEnlisted _) => model with
	{
		AttendeeCount = model.AttendeeCount + 1,
		AttendeeNames = model.AttendeeNames.Concat(new[] { _.StudentName}).ToArray(),
	};
}

/// <summary>
/// A view model for the Course aggregate (identified by the Course ID). 
/// </summary>
public record CourseViewModel(Guid Id, string Name, uint AttendeeCount, IReadOnlyCollection<string> AttendeeNames);
