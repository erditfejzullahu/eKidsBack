using AutoMapper;
using Database.DTOs;
using Database.Models;
using Microsoft.IdentityModel.Tokens;

namespace eKids.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)) FOR ALL DTOS
            //.ForMember(dest => dest.LessonFeaturedImage, opt => opt.Ignore()); Ignore SPECIFIC DTO

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
        }
    }
}
