using Common.Models;
using HotChocolate.Data.Sorting;

namespace CommentSystem.GraphQL.Types
{
    public class CommentSortType : SortInputType<Comment>
    {
        protected override void Configure(ISortInputTypeDescriptor<Comment> descriptor)
        {
            descriptor.Ignore(t => t.Replies);
            descriptor.Field(t => t.CreatedAt);
            descriptor.Field(t => t.User).Type<UserSortType>();
        }
    }
}
