using AutoMapper;
using CleanArq.Application.DTOs;
using CleanArq.Application.Products.Commands;
using CleanArq.Domain.Entities;

namespace CleanArq.Application
{
    public class DomainToDTOMappingProfile : Profile
    {
        public DomainToDTOMappingProfile()
        {
            CreateMap<Product, ProductDTO>();
            CreateMap<ProductDTO, ProductCreateCommand>();
            CreateMap<ProductDTO, PruductUpdateCommand>();
            CreateMap<Category, CategoryDTO>().ReverseMap();
        }
    }
}
