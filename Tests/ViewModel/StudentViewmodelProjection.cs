using Domain.EventSourcing;
using Marten.Events.Aggregation;

namespace MartenPresentation.Tests.ViewModel;

public class StudentViewmodelProjection : SingleStreamProjection<StudentViewModel>
{
	public static StudentViewModel Create(StudentCreated studentCreated) => new(studentCreated.Id, studentCreated.Name, 0);

	public StudentViewModel Apply(StudentViewModel model, StudentEnlistedInCourse _) => model with { NumberOfEnlistedCourses = model.NumberOfEnlistedCourses + 1 };
	public StudentViewModel Apply(StudentViewModel model, StudentDelistedFromCourse _) => model with { NumberOfEnlistedCourses = model.NumberOfEnlistedCourses - 1 };
}

public record StudentViewModel(Guid Id, string Name, uint NumberOfEnlistedCourses);