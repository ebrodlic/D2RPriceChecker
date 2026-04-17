namespace Tests
{
    public class BasicTests
    {
        [Fact]
        public void BasicMath_Works()
        {
            var result = 2 + 2;

            Assert.Equal(4, result);
        }
    }
}
