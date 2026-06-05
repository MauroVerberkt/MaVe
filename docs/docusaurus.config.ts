import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Parses YAML frontmatter between --- delimiters.
 */
function parseFrontmatter(content: string): Record<string, string | string[]> {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  if (!match) return {};

  const yaml = match[1];
  const result: Record<string, string | string[]> = {};

  for (const rawLine of yaml.split('\n')) {
    const line = rawLine.replace(/\r$/, '');
    const kvMatch = line.match(/^(\w+):\s*(.+)$/);
    if (kvMatch) {
      const [, key, value] = kvMatch;
      const arrayMatch = value.match(/^\[(.*)\]$/);
      if (arrayMatch) {
        result[key] = arrayMatch[1].split(',').map((s) => s.trim().replace(/^["']|["']$/g, ''));
      } else {
        result[key] = value.replace(/^["']|["']$/g, '');
      }
    }
  }

  return result;
}

const config: Config = {
  title: 'MaVe',
  tagline: 'Functional patterns for .NET: Result, Option, and Business Rules',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://mauroverberkt.github.io',
  baseUrl: '/MaVe/',

  organizationName: 'MauroVerberkt',
  projectName: 'MaVe',

  onBrokenLinks: 'throw',

  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
    mermaid: true,
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/MauroVerberkt/MaVe/tree/main/docs/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themes: ['@docusaurus/theme-mermaid'],

  plugins: [
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'architecture',
        path: 'architecture',
        routeBasePath: 'architecture',
        sidebarPath: './sidebarsArchitecture.ts',
        editUrl: 'https://github.com/MauroVerberkt/MaVe/tree/main/docs/',
        tags: 'tags.yml',
      },
    ],
    function architectureDocsDataPlugin(context) {
      return {
        name: 'architecture-docs-data',
        async contentLoaded({actions}) {
          const {setGlobalData} = actions;
          const architecturePath = path.resolve(context.siteDir, 'architecture');
          const docs: Array<{
            id: string;
            title: string;
            description: string;
            permalink: string;
            tags: string[];
            category: string;
          }> = [];

          function scanDirectory(dirPath: string, category: string, subDir?: string) {
            if (!fs.existsSync(dirPath)) return;

            const entries = fs.readdirSync(dirPath, {withFileTypes: true});

            for (const entry of entries) {
              if (entry.isDirectory()) {
                scanDirectory(path.join(dirPath, entry.name), category, entry.name);
              } else if (entry.name.endsWith('.md') || entry.name.endsWith('.mdx')) {
                if (entry.name.startsWith('_')) continue;

                const filePath = path.join(dirPath, entry.name);
                const content = fs.readFileSync(filePath, 'utf-8');
                const frontmatter = parseFrontmatter(content);

                const slug = entry.name.replace(/\.(md|mdx)$/, '').replace(/^\d+-/, '');
                const pathSegment = subDir ? `${subDir}/${slug}` : slug;
                const id = `${category}/${pathSegment}`;
                const permalink = `/architecture/${category}/${pathSegment}`;

                let description = (frontmatter.description as string) || '';
                if (!description) {
                  const bodyContent = content.split('---').slice(2).join('---').trim();
                  const lines = bodyContent.split('\n');
                  const firstParagraph = lines
                    .filter((l) => l.trim() && !l.startsWith('#') && !l.startsWith('**Status'))
                    .slice(0, 2)
                    .join(' ')
                    .trim();
                  description = firstParagraph.slice(0, 200);
                }

                docs.push({
                  id,
                  title: (frontmatter.title as string) || slug,
                  description,
                  permalink,
                  tags: Array.isArray(frontmatter.tags) ? frontmatter.tags : [],
                  category,
                });
              }
            }
          }

          for (const category of ['proposals', 'decisions', 'design', 'ways-of-working']) {
            scanDirectory(path.join(architecturePath, category), category);
          }

          setGlobalData({docs});
        },
      };
    },
  ],

  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'MaVe',
      logo: {
        alt: 'MaVe Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'docSidebar',
          sidebarId: 'architectureSidebar',
          docsPluginId: 'architecture',
          position: 'left',
          label: 'Architecture',
        },
        {
          href: 'https://github.com/MauroVerberkt/MaVe',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/getting-started/installation',
            },
            {
              label: 'Result Monad',
              to: '/docs/monads/result/',
            },
            {
              label: 'Business Rules',
              to: '/docs/business-rules/overview',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'Architecture',
              to: '/architecture/overview',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/MauroVerberkt/MaVe',
            },
          ],
        },
      ],
      copyright: `Copyright \u00A9 ${new Date().getFullYear()} MaVe. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'powershell', 'json'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
