using AutoMapper;
using SqlAPI.Models;
using SqlAPI.DTOs;

namespace SqlAPI.Profiles
{
    public class PersonProfile : Profile
    {
        public PersonProfile()
        {
            // Source -> Target
            CreateMap<Person, PersonDto>();
            CreateMap<PersonDto, Person>();
        }
    }
}
