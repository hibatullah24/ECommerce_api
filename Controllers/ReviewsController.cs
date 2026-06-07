using ECommerce_api;
using ECommerce_api.DTOs;
using ECommerce_api.Models;
using ECommerce_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce_api_api.Controllers
{
    [ApiController]
    [Route("api/Reviews")]
    public class ReviewsController : ControllerBase
    {
       
        private string? reviewId;
        public ApplicationDbContext _context;
        public LoggingService<ReviewsController> _logger;

        public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = new LoggingService<ReviewsController>(logger);
        }

        [Authorize]
        [HttpPost("AddReview")]
        public IActionResult AddReview( AddReviewRequest request)
        {
          

            var product = _context.Products.Find(request.PId);
            if (product == null)
            {
                _logger.LogWarning("AddReview failed: Product {ProductId} not found.", request.PId);
                return NotFound("Product not found.");
            }


            //user cannot review the same product more than once.
            var existingReview = _context.Reviews.FirstOrDefault(r => r.PId == request.PId && r.UId == request.UId);
            if (existingReview != null)
            {
                _logger.LogWarning("AddReview failed: User {UserId} already reviewed Product {ProductId}.", request.UId, request.PId);
                return BadRequest("You have already reviewed this product");
            }

            //A user can only review a product they have purchased.
            var hasPurchased = _context.OrderProducts.Any(op => op.PId == request.PId && op.Order.UId == request.UId);
            if (!hasPurchased)
            {
                _logger.LogWarning("AddReview failed: User {UserId} has not purchased Product {ProductId}.", request.UId, request.PId);
                return BadRequest("You can only review products you have purchased.");
            }

            var user = _context.Users.Find(request.UId);
            if (user == null)
                 return NotFound("User not found.");

            //Rating must be between 1 and 5.
            if (request.Rating<1 || request.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            if(string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest("Comment is required.");

            var review = new Review
            {
                UId = request.UId,
                PId = request.PId,
                Rating = request.Rating,
                Comment = request.Comment,
                ReviewDate = DateTime.Now

            };
                

           
            _context.Reviews.Add(review);
            recalculateProductRating(request.PId);
            _context.SaveChanges();

            _logger.LogInfo("User {UserId} added review for Product {ProductId}.", request.UId, request.PId);
            return Ok(new {message = "Review added successfully.", reviewId = review.RId});

        }

        [Authorize]
        [HttpPut("EditReview")]
        public IActionResult EditReview( int Reviewid, int userId, EditReviewRequest request)
        {


            var review = _context.Reviews.FirstOrDefault(rv => rv.RId == Reviewid);
            if (review == null)
            {
                _logger.LogWarning("EditReview failed: Review {ReviewId} not found.", reviewId);
                return NotFound("Review not found.");
            }

            if (review.UId != userId)
            {
                _logger.LogWarning("EditReview failed: User {UserId} tried to edit Review {ReviewId} they don't own.", userId, reviewId);
                return Unauthorized("You can only edit your own reviews");
            }

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest("Comment is required");

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            _context.Reviews.Update(review);

            recalculateProductRating(review.PId);
            _context.SaveChanges();



            _logger.LogInfo("User {UserId} edited Review {ReviewId}.", userId, reviewId);
            return Ok("Review update successfully");
        }

        [Authorize]
        [HttpDelete("DeleteReview")]
        public IActionResult DeleteReview( int Reviewid, int userId)
        {
 

            var review = _context.Reviews.FirstOrDefault(r => r.RId == Reviewid);
            if (review == null)
            {
                _logger.LogWarning("DeleteReview failed: Review {ReviewId} not found.", reviewId);
                return NotFound("Review not found.");

            }

            if (review.UId != userId)
            {
                _logger.LogWarning("DeleteReview failed: User {UserId} tried to delete Review {ReviewId} they don't own.", userId, reviewId);
                return Unauthorized("You can only delete your own reviews");
            }

            int productId = review.PId;
            _context.Reviews.Remove(review);

            recalculateProductRating(productId);
            _context.SaveChanges();

            _logger.LogInfo("User {UserId} deleted Review {ReviewId}.", userId, reviewId);
            return Ok("Review deleted successfully");

        }

        [AllowAnonymous]
        [HttpGet("GetProductReviews")]
        public IActionResult GetProductReviews( int productId, int page = 1)
        {


            var product = _context.Products.Find(productId);
            if (product == null)
            {
                _logger.LogWarning("GetProductReviews: Product {ProductId} not found.", productId);
                return NotFound("Product not found.");
            }

            const int pageSize = 5;
            int total = _context.Reviews.Count(r => r.PId == productId);
            if (total == 0)
            {
                _logger.LogWarning("GetProductReviews: No reviews for Product {ProductId}.", productId);
                return NotFound("No reviews found for this product.");
            }

            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (page < 1 || page > totalPages)
                return BadRequest($"Invalid page. Valid range: 1 - {totalPages}.");

            var reviews = _context.Reviews
                .Where(r => r.PId == productId)
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new          
                {
                    reviewId = r.RId,
                    userId = r.UId,
                    rating = r.Rating,
                    comment = r.Comment,
                    reviewDate = r.ReviewDate
                })
                .ToList();

            _logger.LogInfo("GetProductReviews: Returned page {Page} for Product {ProductId}.", page, productId);


            return Ok(new
            {
                overallRating = product.OverallRating,   
                totalReviews = total,
                page,
                totalPages,
                reviews
            });
        }

        [NonAction]
        public void recalculateProductRating(int productId)
        {
            var product = _context.Products.Find(productId);
            if (productId == 0) return;

            var reviews = _context.Reviews.Where(r => r.PId == productId).ToList();

            product.overriddenRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating),1) : 0;
            _context.Products.Update(product);
        }

       

       


    }
}
