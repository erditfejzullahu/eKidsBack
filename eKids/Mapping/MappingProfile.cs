using AutoMapper;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.IdentityModel.Tokens;

namespace eKids.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)) FOR ALL DTOS
            //.ForMember(dest => dest.LessonFeaturedImage, opt => opt.Ignore()); Ignore SPECIFIC DTO

            //mapfrom instead of condition can set values for a field if condition is not mets

            CreateMap<UpdateLessons, Lessons>()
                .ForMember(dest => dest.LessonName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LessonName)))
                .ForMember(dest => dest.LessonContent, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LessonContent)))
                .ForMember(dest => dest.LessonExcerpt, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LessonExcerpt)))
                .ForMember(dest => dest.LessonType, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LessonType)))
                .ForMember(dest => dest.LessonQuestions, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LessonQuestions)))
                .ForMember(dest => dest.CorrectAnswers, opt => opt.Condition(src => !string.IsNullOrEmpty(src.CorrectAnswers)))
                .ForMember(dest => dest.LessonFeaturedImage, opt => opt.Ignore())
                .ForMember(dest => dest.CourseID, opt => opt.Condition(src => src.CourseID > 0))
                .ForMember(dest => dest.LessonVideo, opt => opt.Ignore());

            CreateMap<UpdateUserProgress, UserProgress>()
                .ForMember(dest => dest.IsCompleted, opt => opt.Condition(src => src.IsCompleted.HasValue))
                .ForMember(dest => dest.HasStarted, opt => opt.Condition(src => src.HasStarted.HasValue));


            CreateMap<UpdateCourses, Courses>()
                .ForMember(dest => dest.CourseName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.CourseName)))
                .ForMember(dest => dest.CourseDescription, opt => opt.Condition(src => !string.IsNullOrEmpty(src.CourseDescription)))
                .ForMember(dest => dest.CourseFeaturedImage, opt => opt.Ignore())
                .ForMember(dest => dest.CourseCategory, opt => opt.Condition(src => src.CourseCategory > 0));

            CreateMap<UpdateUser, Users>()
                .ForMember(dest => dest.Firstname, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Firstname)))
                .ForMember(dest => dest.Lastname, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Lastname)))
                .ForMember(dest => dest.Username, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Username)))
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Role)));

            CreateMap<UserInformationsDto, UserInformations>()
                //.ForMember(dest => dest.Birthday, opt => opt.Condition(src => src.Birthday != default))
                .ForMember(dest => dest.SoftSkills, opt => opt.Condition(src => !string.IsNullOrEmpty(src.SoftSkills)))
                .ForMember(dest => dest.Skills, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Skills)));

            CreateMap<UserEducationsDto, UserEducations>()
                .ForMember(dest => dest.Place_Name, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Place_Name)))
                .ForMember(dest => dest.School_Degree, opt => opt.Condition(src => Enum.TryParse<SchoolDegrees>(src.SchoolDegree.ToString(), out _)))
                .ForMember(dest => dest.Field, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Field)))
                //.ForMember(dest => dest.Start_Year, opt =>
                //{
                //    opt.MapFrom(src => src.Start_Year);
                //    opt.Condition(src => src.Start_Year && src.Start_Year > 1900);
                //})
                .ForMember(dest => dest.Start_Year, opt => opt.MapFrom(src => src.Start_Year)) // add automatically
                .ForMember(dest => dest.End_Year, opt =>
                {
                    opt.MapFrom(src => src.End_Year);
                    opt.Condition(src => src.End_Year.HasValue && src.End_Year.Value > 1900);
                });

            CreateMap<UserJobsDto, UserJobs>()
                .ForMember(dest => dest.Job_Place, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Job_Place)))
                .ForMember(dest => dest.Job_Title, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Job_Title)))
                //.ForMember(dest => dest.Start_Year, opt =>
                //{
                //    opt.MapFrom(src => src.Start_Year);
                //    opt.Condition(src => src.Start_Year.HasValue && src.Start_Year.Value > 1900);
                //})
                .ForMember(dest => dest.Start_Year, opt => opt.MapFrom(src => src.Start_Year)) // add automatically
                .ForMember(dest => dest.End_Year, opt =>
                {
                    opt.MapFrom((src, dest ) => src.End_Year);
                    opt.Condition(src => src.End_Year.HasValue && src.End_Year.Value > 1900);
                });

        }
    }
}
