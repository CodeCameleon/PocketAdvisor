// Renders nginx/default.conf.template into the nginx server configuration, with
// the apiUrl of the environment file that the given Angular build configuration
// compiles into the bundle. Usage: node nginx/render-config.mjs <configuration> <output>
import { readFileSync, writeFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const [configuration, output] = process.argv.slice(2);
const angular = JSON.parse(readFileSync('angular.json', 'utf8'));
const build = angular.projects['PocketAdvisor.Client'].architect.build;
if (!build.configurations[configuration]) {
  throw new Error(`Unknown Angular build configuration: ${configuration}`);
}

// The same file replacement the Angular build applies (see angular.json).
const environmentFile = 'src/environments/environment.ts';
const replacement = (build.configurations[configuration].fileReplacements ?? [])
  .find((fileReplacement) => fileReplacement.replace === environmentFile);
const { environment } = await import(pathToFileURL(replacement?.with ?? environmentFile).href);

if (typeof environment.apiUrl !== 'string' || !/^https?:\/\/\S+[^/]$/.test(environment.apiUrl)) {
  throw new Error(`Invalid apiUrl in the ${configuration} environment: ${environment.apiUrl}`);
}

const template = readFileSync('nginx/default.conf.template', 'utf8');
writeFileSync(output, template.replaceAll('${API_URL}', environment.apiUrl));
console.log(`nginx configuration rendered for ${configuration}: connect-src ${environment.apiUrl}/`);
