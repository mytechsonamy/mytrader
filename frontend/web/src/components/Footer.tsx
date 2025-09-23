import React from 'react';
import './Footer.css';

interface FooterProps {
  className?: string;
}

const Footer: React.FC<FooterProps> = ({ className = '' }) => {
  const currentYear = new Date().getFullYear();

  const handleLinkClick = (e: React.MouseEvent<HTMLAnchorElement>, href: string) => {
    e.preventDefault();
    // For now, just log the link click - you can implement actual routing later
    console.log(`Navigate to: ${href}`);
  };

  return (
    <footer className={`techsonamy-footer ${className}`} role="contentinfo">
      <div className="footer-container">
        {/* Brand Statement Section */}
        <div className="footer-brand">
          <div className="brand-statement">
            <div className="brand-logo-footer">
              <div className="brand-icon-footer">T</div>
              <div className="brand-text">
                <h3 className="footer-title">myTrader</h3>
                <p className="footer-subtitle">Professional Trading Platform by Techsonamy</p>
              </div>
            </div>
          </div>

          <div className="company-description">
            <p>
              Techsonamy is a technology company specializing in professional financial applications
              and trading platforms. myTrader is built and maintained by Techsonamy's experienced
              team of financial technology specialists.
            </p>
          </div>
        </div>

        {/* Contact and Links Section */}
        <div className="footer-content">
          <div className="footer-section">
            <h4 className="footer-section-title">Contact</h4>
            <div className="contact-info">
              <p className="contact-item">
                <span className="contact-label">Website:</span>
                <a
                  href="https://www.techsonamy.com"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="contact-link"
                >
                  www.techsonamy.com
                </a>
              </p>
              <p className="contact-item">
                <span className="contact-label">Support:</span>
                <a
                  href="mailto:support@techsonamy.com"
                  className="contact-link"
                >
                  support@techsonamy.com
                </a>
              </p>
              <p className="contact-item">
                <span className="contact-label">Business:</span>
                <a
                  href="mailto:info@techsonamy.com"
                  className="contact-link"
                >
                  info@techsonamy.com
                </a>
              </p>
            </div>
          </div>

          <div className="footer-section">
            <h4 className="footer-section-title">Platform</h4>
            <nav className="footer-nav" role="navigation" aria-label="Footer Navigation">
              <ul className="footer-nav-list">
                <li>
                  <a
                    href="/features"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/features')}
                  >
                    Features
                  </a>
                </li>
                <li>
                  <a
                    href="/pricing"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/pricing')}
                  >
                    Pricing
                  </a>
                </li>
                <li>
                  <a
                    href="/documentation"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/documentation')}
                  >
                    Documentation
                  </a>
                </li>
                <li>
                  <a
                    href="/api"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/api')}
                  >
                    API Access
                  </a>
                </li>
              </ul>
            </nav>
          </div>

          <div className="footer-section">
            <h4 className="footer-section-title">Support</h4>
            <nav className="footer-nav" role="navigation" aria-label="Support Navigation">
              <ul className="footer-nav-list">
                <li>
                  <a
                    href="/help"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/help')}
                  >
                    Help Center
                  </a>
                </li>
                <li>
                  <a
                    href="/contact"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/contact')}
                  >
                    Contact Us
                  </a>
                </li>
                <li>
                  <a
                    href="/status"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/status')}
                  >
                    System Status
                  </a>
                </li>
                <li>
                  <a
                    href="/community"
                    className="footer-nav-link"
                    onClick={(e) => handleLinkClick(e, '/community')}
                  >
                    Community
                  </a>
                </li>
              </ul>
            </nav>
          </div>
        </div>

        {/* Trust Indicators */}
        <div className="footer-trust">
          <div className="trust-indicators">
            <div className="trust-item">
              <div className="trust-icon">🔒</div>
              <div className="trust-content">
                <span className="trust-title">Enterprise Security</span>
                <span className="trust-description">Bank-grade encryption</span>
              </div>
            </div>
            <div className="trust-item">
              <div className="trust-icon">📈</div>
              <div className="trust-content">
                <span className="trust-title">Professional Platform</span>
                <span className="trust-description">Institutional grade tools</span>
              </div>
            </div>
            <div className="trust-item">
              <div className="trust-icon">⚡</div>
              <div className="trust-content">
                <span className="trust-title">Real-time Data</span>
                <span className="trust-description">Live market feeds</span>
              </div>
            </div>
          </div>
        </div>

        {/* Legal and Copyright */}
        <div className="footer-legal">
          <div className="copyright">
            <p>&copy; {currentYear} Techsonamy. All rights reserved.</p>
          </div>

          <div className="legal-links">
            <nav role="navigation" aria-label="Legal Navigation">
              <ul className="legal-nav-list">
                <li>
                  <a
                    href="/privacy"
                    className="legal-link"
                    onClick={(e) => handleLinkClick(e, '/privacy')}
                  >
                    Privacy Policy
                  </a>
                </li>
                <li>
                  <a
                    href="/terms"
                    className="legal-link"
                    onClick={(e) => handleLinkClick(e, '/terms')}
                  >
                    Terms of Service
                  </a>
                </li>
                <li>
                  <a
                    href="/compliance"
                    className="legal-link"
                    onClick={(e) => handleLinkClick(e, '/compliance')}
                  >
                    Compliance
                  </a>
                </li>
                <li>
                  <a
                    href="/cookies"
                    className="legal-link"
                    onClick={(e) => handleLinkClick(e, '/cookies')}
                  >
                    Cookie Policy
                  </a>
                </li>
              </ul>
            </nav>
          </div>

          <div className="powered-by">
            <span className="powered-text">Powered by</span>
            <a
              href="https://www.techsonamy.com"
              target="_blank"
              rel="noopener noreferrer"
              className="techsonamy-link"
              aria-label="Visit Techsonamy website"
            >
              <strong>Techsonamy</strong>
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;