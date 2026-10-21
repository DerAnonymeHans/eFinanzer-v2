interface LogoProps {
  size: number;
}

// Decorative, the "eFinanzer" text next to it already names the app
function Logo({ size }: LogoProps) {
  return (
    <img
      className="app-logo"
      src={`${import.meta.env.BASE_URL}money-with-wings.svg`}
      width={size}
      height={size}
      alt=""
      aria-hidden="true"
    />
  );
}

export default Logo;
