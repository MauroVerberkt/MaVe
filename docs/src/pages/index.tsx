import {useState, type ReactNode} from 'react';
import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function BeforeCode() {
  return (
    <pre className={styles.syntaxBlock}><code>
<span className={styles.synKeyword}>try</span>{'\n'}
<span className={styles.synPunct}>{'{'}</span>{'\n'}
{'    '}<span className={styles.synKeyword}>var</span> <span className={styles.synVar}>user</span> <span className={styles.synPunct}>=</span> <span className={styles.synKeyword}>await</span> <span className={styles.synVar}>repository</span><span className={styles.synPunct}>.</span><span className={styles.synMethod}>GetById</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>id</span><span className={styles.synPunct}>);</span>{'\n'}
{'\n'}
{'    '}<span className={styles.synKeyword}>if</span> <span className={styles.synPunct}>(</span><span className={styles.synVar}>user</span> <span className={styles.synKeyword}>is</span> <span className={styles.synKeyword}>null</span><span className={styles.synPunct}>)</span>{'\n'}
{'        '}<span className={styles.synKeyword}>throw new</span> <span className={styles.synType}>UserNotFoundException</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>id</span><span className={styles.synPunct}>);</span>{'\n'}
{'\n'}
{'    '}<span className={styles.synKeyword}>var</span> <span className={styles.synVar}>order</span> <span className={styles.synPunct}>=</span> <span className={styles.synKeyword}>await</span> <span className={styles.synVar}>orderService</span><span className={styles.synPunct}>.</span><span className={styles.synMethod}>Create</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>user</span><span className={styles.synPunct}>);</span>{'\n'}
{'\n'}
{'    '}<span className={styles.synKeyword}>await</span> <span className={styles.synVar}>paymentService</span><span className={styles.synPunct}>.</span><span className={styles.synMethod}>Process</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>order</span><span className={styles.synPunct}>);</span>{'\n'}
{'\n'}
{'    '}<span className={styles.synKeyword}>return</span> <span className={styles.synVar}>order</span><span className={styles.synPunct}>;</span>{'\n'}
<span className={styles.synPunct}>{'}'}</span>{'\n'}
<span className={styles.synKeyword}>catch</span> <span className={styles.synPunct}>(</span><span className={styles.synType}>ValidationException</span> <span className={styles.synVar}>ex</span><span className={styles.synPunct}>)</span>{'\n'}
<span className={styles.synPunct}>{'{'}</span>{'\n'}
{'    '}<span className={styles.synVar}>logger</span><span className={styles.synPunct}>.</span><span className={styles.synMethod}>LogWarning</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>ex</span><span className={styles.synPunct}>);</span>{'\n'}
{'    '}<span className={styles.synKeyword}>throw</span><span className={styles.synPunct}>;</span>{'\n'}
<span className={styles.synPunct}>{'}'}</span>{'\n'}
<span className={styles.synKeyword}>catch</span> <span className={styles.synPunct}>(</span><span className={styles.synType}>PaymentException</span> <span className={styles.synVar}>ex</span><span className={styles.synPunct}>)</span>{'\n'}
<span className={styles.synPunct}>{'{'}</span>{'\n'}
{'    '}<span className={styles.synVar}>logger</span><span className={styles.synPunct}>.</span><span className={styles.synMethod}>LogError</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>ex</span><span className={styles.synPunct}>);</span>{'\n'}
{'    '}<span className={styles.synKeyword}>throw</span><span className={styles.synPunct}>;</span>{'\n'}
<span className={styles.synPunct}>{'}'}</span>
    </code></pre>
  );
}

function AfterCode() {
  return (
    <pre className={styles.syntaxBlock}><code>
<span className={styles.synKeyword}>return await</span> <span className={styles.synMethod}>GetUser</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>id</span><span className={styles.synPunct}>)</span>{'\n'}
{'    '}<span className={styles.synPunct}>.</span><span className={styles.synMethod}>BindAndTransformAsync</span><span className={styles.synPunct}>(</span>{'\n'}
{'        '}<span className={styles.synVar}>u</span> <span className={styles.synPunct}>=&gt;</span> <span className={styles.synMethod}>CreateOrder</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>u</span><span className={styles.synPunct}>))</span>{'\n'}
{'    '}<span className={styles.synPunct}>.</span><span className={styles.synMethod}>BindAndTransformAsync</span><span className={styles.synPunct}>(</span>{'\n'}
{'        '}<span className={styles.synVar}>o</span> <span className={styles.synPunct}>=&gt;</span> <span className={styles.synMethod}>ProcessPayment</span><span className={styles.synPunct}>(</span><span className={styles.synVar}>o</span><span className={styles.synPunct}>));</span>{'\n\n\n\n\n'}
    </code></pre>
  );
}

function Hero() {
  return (
    <section className={styles.hero}>
      <span className={styles.heroLabel}>.NET LIBRARY COLLECTION</span>
      <Heading as="h1" className={styles.heroTitle}>
        <span>Explicit failures.</span>
        <span>Source-generated rules.</span>
        <span className={styles.heroTitleMuted}>No reflection.</span>
      </Heading>
      <p className={styles.heroSubtitle}>
        Composable foundations for modern .NET.
      </p>
      <div className={styles.heroCta}>
        <Link className={styles.ctaPrimary} to="/docs/getting-started/installation">
          Explore Docs
        </Link>
        <span className={styles.ctaSecondary}>
          or view on{' '}
          <Link href="https://github.com/MauroVerberkt/MaVe">GitHub</Link>
        </span>
      </div>
    </section>
  );
}

function CodeArtifact() {
  const [showAfter, setShowAfter] = useState(false);

  return (
    <section className={styles.artifact}>
      <div className={styles.artifactCode}>
        <div className={styles.codeWindow}>
          <div className={styles.windowDots}>
            <span /><span /><span />
          </div>
          <span className={styles.windowTitle}>OrderService.cs</span>
          <button
            className={styles.codeToggle}
            onClick={() => setShowAfter(!showAfter)}
            type="button"
          >
            {showAfter ? '← See the problem' : 'Fix it →'}
          </button>
        </div>
        <div className={styles.codeContent}>
          <div className={styles.codeInner}>
            <span className={showAfter ? styles.codeCommentGreen : styles.codeComment}>
              {showAfter
                ? '// Explicit, composable, type-safe'
                : '// Hidden control flow, scattered try/catch'}
            </span>
            {showAfter ? <AfterCode /> : <BeforeCode />}
          </div>
        </div>
      </div>
      <aside className={styles.artifactAdr}>
        <span className={styles.adrLabel}>ADR-001</span>
        <Heading as="h3" className={styles.adrTitle}>
          Result Pattern Over Exceptions
        </Heading>
        <span className={styles.adrStatus}>Accepted</span>
        <p className={styles.adrExcerpt}>
          In traditional C# code, errors are communicated through exceptions.
          When used for expected failures, they become control flow — hidden from
          method signatures and impossible to compose.
        </p>
        <Link className={styles.adrLink} to="/architecture/decisions/result-over-exceptions">
          Read the decision →
        </Link>
      </aside>
    </section>
  );
}

function Approach() {
  return (
    <section className={styles.approach}>
      <span className={styles.sectionLabel}>APPROACH</span>
      <Heading as="h2" className={styles.approachTitle}>
        The type system as your safety net.
      </Heading>
      <div className={styles.approachBody}>
        <p>
          Operations return Result&lt;T&gt; instead of throwing. Values use Option&lt;T&gt; instead of null.
          Domain alternatives are modeled as source-generated discriminated unions with exhaustive matching.
          Business rules are defined in JSON and generated as strongly-typed C# classes at compile time.
          Operation dispatch at JSON boundaries is generated from a single annotated class — no registration, no routing config.
          Roslyn analyzers catch violations before code runs. Source generators eliminate reflection.
        </p>
        <p className={styles.approachMuted}>
          Packages depend on each other only where the design demands it — never by accident.
        </p>
      </div>
    </section>
  );
}

function Packages() {
  return (
    <section className={styles.packages}>
      <span className={styles.sectionLabel}>PACKAGES</span>
      <div className={styles.packagesPrimary}>
        <div className={styles.packageItem}>
          <Heading as="h3">Monads</Heading>
          <p>Result&lt;T&gt; and Option&lt;T&gt; with full async composition, CancellationToken support, and monadic chaining</p>
        </div>
        <div className={styles.packageItem}>
          <Heading as="h3">BusinessRules</Heading>
          <p>JSON-defined rules → source-generated C# classes + Roslyn analyzers for compile-time validation</p>
        </div>
        <div className={styles.packageItem}>
          <Heading as="h3">Unions</Heading>
          <p>Source-generated discriminated unions with exhaustive Match/Switch builders and compile-time analyzer support</p>
        </div>
        <div className={styles.packageItem}>
          <Heading as="h3">Railyard</Heading>
          <p>Compile-time generated operation dispatch for JSON payload boundaries — one annotated class, zero registration boilerplate</p>
        </div>
      </div>
      <div className={styles.packagesSecondary}>
        <div className={styles.packageSecondaryItem}>
          <span className={styles.packageName}>BusinessRules.ResultExtensions</span>
          <span className={styles.packageDesc}>— validation → Result bridge</span>
        </div>
        <div className={styles.packageSecondaryItem}>
          <span className={styles.packageName}>BusinessRules.Wcf</span>
          <span className={styles.packageDesc}>— WCF FaultException integration</span>
        </div>
      </div>
    </section>
  );
}

function PageFooter() {
  return (
    <section className={styles.pageFooter}>
      <div className={styles.metrics}>
        <a href="https://github.com/MauroVerberkt/MaVe/actions/workflows/ci.yml" className={styles.metricLive}>
          <span className={styles.metricDot} />
          CI passing
        </a>
        <a href="https://app.codecov.io/github/MauroVerberkt/MaVe" className={styles.metricLive}>
          <span className={styles.metricDot} />
          Coverage
        </a>
        <span className={styles.metricStatic}>.NET 8.0+</span>
        <span className={styles.metricStatic}>MIT</span>
      </div>
      <div className={styles.signoff}>
        <p className={styles.signoffTagline}>Clear architecture. Confident execution.</p>
        <p className={styles.signoffCopyright}>© 2026 Mauro Verberkt</p>
      </div>
    </section>
  );
}

export default function Home(): ReactNode {
  return (
    <Layout
      title="Home"
      description="Production-grade functional building blocks for .NET"
      noFooter>
      <main className={styles.landing}>
        <Hero />
        <CodeArtifact />
        <Approach />
        <Packages />
        <PageFooter />
      </main>
    </Layout>
  );
}
