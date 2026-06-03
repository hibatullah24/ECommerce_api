using E_Commerce_System;
using E_Commerce_System.Models;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_system_api.Controllers
{
    [ApiController]
    [Route("api/Reviews")]
    public class ReviewsController : ControllerBase
    {
        ApplicationDbContext context = new ApplicationDbContext();

        [HttpPost("AddReview")]
        public IActionResult AddReview(AddReviewRequest request)
        {
            var product = context.Products.Find(request.PId);
            if (product == null)
                return NotFound("Product not found.");


            //user cannot review the same product more than once.
            var existingReview = context.Reviews.FirstOrDefault(r => r.PId == request.PId && r.UId == request.UId);
            if (existingReview != null)
                return BadRequest("You have already reviewed this product");

            //A user can only review a product they have purchased.
            var hasPurchased = context.OrderProducts.Any(op => op.PId == request.PId && op.Order.UId == request.UId);
            if (!hasPurchased)
                return BadRequest("You can only review products you have purchased.");

            var user = context.Users.Find(request.UId);
            if (user == null)
                ; return NotFound("User not found.");

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
                

           
            context.Reviews.Add(review);
            context.SaveChanges();
            return Ok("Review added successfully.");

        }

        [HttpPut("EditReview")]
        public IActionResult EditReview(int Reviewid, int userId, EditReviewRequest request)
        {
            var review = context.Reviews.FirstOrDefault(rv => rv.RId == Reviewid);
            if (review == null)
                return NotFound("Review not found.");

            if (review.UId != userId)
                return Unauthorized("You can only edit your own reviews");

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest("Comment is required");

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            context.Reviews.Update(review);
            context.SaveChanges();

            return Ok("Review update successfully");
        }

        [HttpDelete("DeleteReview")]
        public IActionResult DeleteReview(int Reviewid, int userId)
        {
            var review = context.Reviews.FirstOrDefault(r => r.RId == Reviewid);
            if (review == null)
                return NotFound("Review not found.");

            if (review.UId != userId)
                return Unauthorized("You can only delete your own reviews");

            context.Reviews.Remove(review);
            context.SaveChanges();
            return Ok("Review deleted successfully");

        }


        [HttpGet("GetProductReviews")]
        public IActionResult GetProductReviews(int productId, int page = 1)
        {
            var product = context.Products.Find(productId);
            if (product == null)
                return NotFound("Product not found.");

            const int pageSize = 5;
            int total = context.Reviews.Count(r => r.PId == productId);
            if (total == 0)
                return NotFound("No reviews found for this product.");

            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (page < 1 || page > totalPages)
                return BadRequest($"Invalid page. Valid range: 1 - {totalPages}.");

            var reviews = context.Reviews
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

            return Ok(reviews);
        }

        public class AddReviewRequest
        {
            public int UId { get; set; }
            public int PId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
        }

        public class EditReviewRequest
        {
            public int Rating { get; set; }
            public string Comment { get; set; }
        }


    }
}
