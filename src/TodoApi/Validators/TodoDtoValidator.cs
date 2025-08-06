using TodoApi.DTOs;
using FluentValidation;

namespace TodoApi.Validators;

public class TodoDtoValidator : AbstractValidator<TodoDTO>
{
    public TodoDtoValidator()
    {
        RuleFor(dto => dto.Name)
        .NotEmpty().WithMessage("Имя задачи не может быть пустым")
        .MaximumLength(100).WithMessage("Имя задачи не может быть длиннее 100 символов");
    }
}