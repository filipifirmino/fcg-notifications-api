using Bogus;
using FCG.Notifications.Domain.Entities;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Infra;
using FCG.Notifications.Infra.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FCG.Notifications.Tests.Unit.Infra.Repositories;

public class NotificationLogRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly NotificationLogRepository _sut;
    private readonly Faker _faker = new("pt_BR");

    public NotificationLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new NotificationLogRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private NotificationLog BuildLog(NotificationType type = NotificationType.Welcome) =>
        NotificationLog.Create(type, _faker.Internet.Email(), _faker.Lorem.Sentence());

    [Fact]
    public async Task GetRecentAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = await _sut.GetRecentAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNotificationLog()
    {
        var log = BuildLog();

        await _sut.AddAsync(log);

        var result = await _sut.GetRecentAsync();
        result.Should().ContainSingle(l => l.Id == log.Id);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnMostRecentFirst()
    {
        var oldest = BuildLog();
        await _sut.AddAsync(oldest);
        var newest = BuildLog(NotificationType.PurchaseConfirmation);
        await _sut.AddAsync(newest);

        var result = await _sut.GetRecentAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newest.Id);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldRespectTakeLimit()
    {
        await _sut.AddAsync(BuildLog());
        await _sut.AddAsync(BuildLog());
        await _sut.AddAsync(BuildLog());

        var result = await _sut.GetRecentAsync(take: 2);

        result.Should().HaveCount(2);
    }
}
