using Common.Models;
using HotChocolate.Data.Sorting;

namespace CommentSystem.GraphQL.Types
{
    public class UserSortType : SortInputType<User>
    {
        protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
        {
            descriptor.Field(t => t.UserName);
            descriptor.Field(t => t.Email);
        }
    }
}
