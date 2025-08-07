using AutoMapper;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.MappingProfiles;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<TodoDTO, Todo>().ReverseMap();

        CreateMap<Todo, TodoViewDto>();
    }
}