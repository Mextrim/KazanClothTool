document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-year]').forEach((element) => {
    element.textContent = new Date().getFullYear();
  });

  const carouselElement = document.getElementById('screenshotsCarousel');

  if (carouselElement && window.bootstrap) {
    window.bootstrap.Carousel.getOrCreateInstance(carouselElement, {
      interval: 5000,
      ride: 'carousel',
      touch: true,
      wrap: true,
    });
  }
});
