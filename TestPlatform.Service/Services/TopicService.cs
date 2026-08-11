using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Topics;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class TopicService : ITopicService
{
    private readonly AppDbContext _context;

    public TopicService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TopicDetailsDto> CreateAsync(TopicCreateDto dto)
    {
        if (await _context.Topics.AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower().Trim()))
        {
            throw new Exception("Bunday nomli mavzu allaqachon mavjud!");
        }

        var topic = new Topic
        {
            Name = dto.Name.Trim(),
            Description = dto.Description
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        return new TopicDetailsDto
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CreatedAt = topic.CreatedAt
        };
    }

    public async Task<TopicDetailsDto> UpdateAsync(Guid id, TopicUpdateDto dto)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null) throw new Exception("Mavzu topilmadi!");

        if (await _context.Topics.AnyAsync(t => t.Id != id && t.Name.ToLower() == dto.Name.ToLower().Trim()))
        {
            throw new Exception("Bunday nomli mavzu allaqachon mavjud!");
        }

        topic.Name = dto.Name.Trim();
        topic.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new TopicDetailsDto
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CreatedAt = topic.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null) return false;

        _context.Topics.Remove(topic);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<TopicDetailsDto?> GetByIdAsync(Guid id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null) return null;

        return new TopicDetailsDto
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CreatedAt = topic.CreatedAt
        };
    }

    public async Task<IEnumerable<TopicDetailsDto>> GetAllAsync()
    {
        var topics = await _context.Topics
            .OrderBy(t => t.Name)
            .ToListAsync();

        return topics.Select(t => new TopicDetailsDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        });
    }
}
