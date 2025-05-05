using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamWebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegasStreamApi_UnitTests.Controllers
{
    public class LiveStreamControllerTests
    {
        private readonly Mock<ILiveStreamRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly LiveStreamController _controller;

        public LiveStreamControllerTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockRepo = new Mock<ILiveStreamRepository>();
            _controller = new LiveStreamController(_mockRepo.Object, _mockMapper.Object);
        }


        [Fact]
        public async Task GetAllStream_ShouldReturnNotFound_WhenRepositoryReturnsNull()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetAllLiveStreamsAsync())
                     .ReturnsAsync((List<LiveStream>)null);

            // Act
            var result = await _controller.GetAllStream();

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetAllStream_ShouldReturnOk_WithEmptyList_WhenNoLiveStreamsExist()
        {
            // Arrange
            var liveStreams = new List<LiveStream>();
            _mockRepo.Setup(repo => repo.GetAllLiveStreamsAsync())
                     .ReturnsAsync(liveStreams);
            _mockMapper.Setup(mapper => mapper.Map<List<LiveStreamDto>>(liveStreams))
                       .Returns(new List<LiveStreamDto>());

            // Act
            var result = await _controller.GetAllStream();

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeOfType<List<LiveStreamDto>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllStream_ShouldReturnOk_WithLiveStreamDtos_WhenLiveStreamsExist()
        {
            // Arrange
            var liveStreams = new List<LiveStream>
            {
                new LiveStream
                {
                    Id = "1",
                    UserId = "user1",
                    StreamTitle = "Test Stream",
                    Description = "Test Description",
                    StartedAt = DateTime.UtcNow
                }
            };
            var liveStreamDtos = new List<LiveStreamDto>
            {
            new LiveStreamDto
                {
                    Id = "1",
                    StreamTitle = "Test Stream",
                    Description = "Test Description"
                }
            };
            _mockRepo.Setup(repo => repo.GetAllLiveStreamsAsync())
                     .ReturnsAsync(liveStreams);
            _mockMapper.Setup(mapper => mapper.Map<List<LiveStreamDto>>(liveStreams))
                       .Returns(liveStreamDtos);

            // Act
            var result = await _controller.GetAllStream();

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(liveStreamDtos);
        }

        [Fact]
        public async Task GetLiveStream_ShouldReturnNotFound_WhenLiveStreamDoesNotExist()
        {
            // Arrange
            var streamId = "999";
            _mockRepo.Setup(repo => repo.GetLiveStreamByIdAsync(streamId))
                     .ReturnsAsync((LiveStream)null);

            // Act
            var result = await _controller.GetLiveStream(streamId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetLiveStream_ShouldReturnOk_WithLiveStreamDto_WhenLiveStreamExists()
        {
            // Arrange
            var streamId = "123";
            var liveStream = new LiveStream
            {
                Id = streamId,
                UserId = "user1",
                StreamTitle = "Test Stream",
                Description = "Test",
                StartedAt = DateTime.UtcNow
            };
            var liveStreamDto = new LiveStreamDto
            {
                Id = streamId,
                StreamTitle = "Test Stream",
                Description = "Test"
            };
            _mockRepo.Setup(repo => repo.GetLiveStreamByIdAsync(streamId))
                     .ReturnsAsync(liveStream);
            _mockMapper.Setup(mapper => mapper.Map<LiveStreamDto>(liveStream))
                       .Returns(liveStreamDto);

            // Act
            var result = await _controller.GetLiveStream(streamId);

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(liveStreamDto);
        }

        [Fact]
        public async Task GetLiveStream_ShouldCallMapperOnce_WhenLiveStreamExists()
        {
            // Arrange
            var streamId = "123";
            var liveStream = new LiveStream
            {
                Id = streamId,
                UserId = "user1",
                StreamTitle = "Stream Title"
            };
            var liveStreamDto = new LiveStreamDto
            {
                Id = streamId,
                StreamTitle = "Stream Title"
            };
            _mockRepo.Setup(repo => repo.GetLiveStreamByIdAsync(streamId))
                     .ReturnsAsync(liveStream);
            _mockMapper.Setup(mapper => mapper.Map<LiveStreamDto>(It.IsAny<LiveStream>()))
                       .Returns(liveStreamDto);

            // Act
            await _controller.GetLiveStream(streamId);

            // Assert
            _mockMapper.Verify(mapper => mapper.Map<LiveStreamDto>(liveStream), Times.Once);
        }
    }
}
