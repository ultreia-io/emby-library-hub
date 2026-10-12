#!/usr/bin/env node
/**
 * Capture the real Library Hub templates/controllers using local, fictional API responses.
 * Every browser request is intercepted; no real Emby account, SMTP, or media server is used.
 * The administration page uses a documented preview stylesheet, not a captured Emby dashboard shell.
 */
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const {chromium} = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(__dirname, '..');
const output = path.join(root, 'docs/assets/screenshots');
const samples = process.env.DOCS_SAMPLES || path.join(root, 'artifacts/docs-samples');
const version = fs.readFileSync(path.join(root, 'Directory.Build.props'), 'utf8').match(/<Version>([^<]+)<\/Version>/)[1];
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8').replaceAll('{{VERSION}}', version);
const origin = 'https://library-hub.example.test';
const prefix = '/emby/LibraryHub';
const configPath = 'src/Emby.LibraryHub/Configuration/';
const standalone = 'src/Emby.LibraryHub/Standalone/';
const screenshotNames = [];
const only = process.argv.find(arg=>arg.startsWith('--only='))?.slice(7);

// Emby hosts the admin controls. This small, clearly labelled host reproduces their
// standard form layout without redistributing Emby's proprietary application assets.
const adminCss = `
*{box-sizing:border-box}body{margin:0;background:#181b19;color:#e4eae5;font:16px/1.55 Arial,sans-serif}
.preview-banner{padding:13px 38px;border-bottom:1px solid #35453a;background:#213b2c;color:#c8e8d2;font-size:13px;letter-spacing:.02em}
.readOnlyContent{max-width:900px;margin:auto;padding:22px 38px 40px}h1{font-size:30px;margin:4px 0 14px}h2{font-size:24px;font-weight:600;margin:32px 0 14px}p{margin:12px 0}a{color:#77cc94;text-decoration:none}
.inputContainer,.selectContainer{margin:22px 0}.inputLabel,.selectLabel{display:block;font-weight:600;margin-bottom:7px}
input,select,button{font:inherit}input:not([type=checkbox]),select{width:100%;color:#edf4ee;background:#222b25;border:1px solid #59675d;border-radius:5px;padding:10px 12px}
input[type=checkbox]{accent-color:#52b54b;width:19px;height:19px;margin:0 10px 0 0;vertical-align:middle}.checkboxContainer{margin:15px 0}
.fieldDescription{font-size:14px;color:#b3bcb6;margin-top:6px}.checkboxContainer-withDescription .fieldDescription{margin-left:29px}
button{color:#eff6f0;background:#35463a;border:0;border-radius:5px;padding:10px 17px;cursor:pointer;margin:4px 6px 4px 0}.button-submit{background:#388a48}.block{display:block;width:100%}
table{font-size:15px}th{padding:12px 10px;border-bottom:1px solid #536358}td{border-bottom:1px solid #35453a}pre{padding:18px;background:#222b25;border-radius:8px}
.hub-footer{padding:18px 38px!important;color:#a7b4ac}.hub-footer a{color:#77cc94}[hidden]{display:none!important}
`;

function libraries(fr) {
  return [{Id:'10',Name:fr?'Cinéma':'Cinema'}, {Id:'30',Name:fr?'Séries':'Series'},
    {Id:'40',Name:'Collections',IsCollections:true}];
}
function subscribers() {
  return [{Id:'demo-1',Email:'alex@example.test',Language:'en',Status:'Subscribed'},
    {Id:'demo-2',Email:'camille@example.test',Language:'fr',Status:'Subscribed'},
    {Id:'demo-3',Email:'sam@example.test',Language:'en',Status:'Awaiting confirmation'}];
}
function adminDocument(lang) {
  const fr = lang === 'fr';
  const data = {
    Digest: {BrowseLibraryIds:['10','30','40'], DeliveryEnabled:true,SubscriptionsEnabled:true,
      Language:lang,TimeZoneId:'Europe/Paris',PublicServerUrl:'https://media.example.test',
      DeliveryHour:8,DeliveryMinute:0,IncludeVideoLinks:true,Sender:'library@example.test',
      SmtpHost:'smtp.example.test',SmtpPort:587,UseStartTls:true,
      SmtpUsername:'library@example.test',SmtpPassword:''}
  };
  return `<!doctype html><html lang="${lang}"><head><meta charset="utf-8"><title>Library Hub · ${fr?'Aperçu administrateur':'Administrator preview'}</title><style>${adminCss}</style></head><body>
  <div class="preview-banner">${fr?'APERÇU LOCAL · DONNÉES FICTIVES · RÉGLAGES DU PLUGIN':'LOCAL PREVIEW · FICTIONAL DATA · PLUGIN SETTINGS'}</div>
  ${read(configPath+'config.html')}
  <script>
  window.Emby={importModule:async()=>({getCurrentLocale:()=>${JSON.stringify(lang)}})};
  window.ApiClient={
    getUrl:(route,q)=>'/emby/'+route+(q?'?'+new URLSearchParams(q):''),
    getPluginConfiguration:async()=>(${JSON.stringify(data)}),
    getJSON:async(route)=>{
      if(route.includes('Browse/Libraries'))return {Libraries:${JSON.stringify(libraries(fr))}};
      if(route.includes('Reports/Calendar'))return {Today:'2026-10-12'};
      if(route.includes('Reports/Link'))return {Path:'emby/LibraryHub/Archive'};
      if(route.includes('/Subscribers'))return {Subscribers:${JSON.stringify(subscribers())}};
      if(route.includes('/Preview'))return {Subject:${JSON.stringify(fr?'Nouveautés du jour':'Today’s library updates')},Body:${JSON.stringify(fr?'Cinéma\nAjouts :\n• Arrival (2016)':'Cinema\nAdded:\n• Arrival (2016)')}};
      throw new Error('Unexpected demo read: '+route);
    },
    updatePluginConfiguration:async()=>{throw new Error('Settings writes are disabled in documentation captures');},
    ajax:async()=>{throw new Error('Administration writes are disabled in documentation captures');}
  };
  window.define=(_,factory)=>{window.demoLocale=factory();};
  </script><script src="/preview/locale.js"></script>
  <script>window.define=(_,factory)=>{
    function BaseView(){} BaseView.prototype.onResume=function(){};
    const View=factory(BaseView,{show(){},hide(){}},window.demoLocale);
    window.demoView=new View(document.getElementById('libraryDigestPage'),{});
    window.demoView.onResume().then(()=>{window.demoReady=true;});
  };</script><script src="/preview/config.js"></script></body></html>`;
}

function browseReply(q) {
  const mode=q.get('Mode'), library=q.get('LibraryId'), group=q.get('Group'), term=q.get('Search');
  if (library !== '10') return {Groups:[],Items:[],Next:null};
  if (mode === 'groups') return {Groups:[{Key:term?'T':'A',Label:term?'T':'A',Kind:'letter',HasGroups:false}],Items:[],Next:null};
  const films = term ? [{Id:'105',Title:'Tinker Tailor Soldier Spy',Kind:'Movie',Year:2011,Minutes:127,Rating:7.0,MatchField:'Actor',MatchValue:'Gary Oldman'}] :
    [{Id:'102',Title:'Amélie',Kind:'Movie',Year:2001,Minutes:122,Rating:8.3},
     {Id:'103',Title:'Apollo 13',Kind:'Movie',Year:1995,Minutes:140,Rating:7.7},
     {Id:'101',Title:'Arrival',Kind:'Movie',Year:2016,Minutes:116,Rating:7.9}];
  return {Groups:[],Items:group?films:[],Next:null};
}

async function fixture(browser, language, mobile=false) {
  const context=await browser.newContext({viewport:mobile?{width:430,height:970}:{width:1120,height:1020},
    locale:language==='fr'?'fr-FR':'en-GB',colorScheme:'light',deviceScaleFactor:1,
    isMobile:mobile,hasTouch:mobile});
  const unexpected=[];
  await context.route('**/*',async route=>{
    const url=new URL(route.request().url());
    if(url.origin!==origin){unexpected.push(url.href);return route.abort();}
    const p=url.pathname;
    const reply=(data)=>route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data)});
    if(p==='/preview/admin')return route.fulfill({contentType:'text/html',body:adminDocument(language)});
    if(p==='/preview/report')return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(samples,'archive.'+language+'.html'),'utf8')});
    if(p==='/preview/config.js'||p==='/preview/locale.js')return route.fulfill({contentType:'application/javascript',body:read(configPath+path.basename(p))});
    if(p.startsWith(prefix+'/Assets/')){
      const name=path.basename(p);
      if(['app.js','browse.js'].includes(name))return route.fulfill({contentType:'application/javascript',body:read(standalone+name)});
      if(['logo.svg','favicon.svg','favicon.png'].includes(name))return route.fulfill({path:path.join(root,'docs/assets',name)});
    }
    if(p==='/emby/System/Info/Public')return reply({Id:'demo-server'});
    if(p===prefix+'/Community/Libraries')return reply({Libraries:libraries(language==='fr'),ServerId:'demo-server'});
    if(p===prefix+'/Community/Browse')return reply(browseReply(url.searchParams));
    if(p===prefix+'/Community/Reports')return reply({Html:fs.readFileSync(path.join(samples,'archive.'+language+'.html'),'utf8')});
    if(p===prefix+'/Community/Subscription' && route.request().method()==='GET')return reply({Enabled:true,FormToken:'fictional-form-proof',Subscriptions:[subscribers()[language==='fr'?1:0]]});
    if(route.request().method()!=='GET'){unexpected.push('Write '+p);return route.abort();}
    if([prefix+'/Browse',prefix+'/Search',prefix+'/Subscriptions',prefix+'/Archive',prefix+'/Archive/2026-10-11'].includes(p))
      return route.fulfill({contentType:'text/html',body:read(standalone+'page.html').replaceAll('{{ROOT}}',prefix)});
    unexpected.push(p);return route.abort();
  });
  const page=await context.newPage();
  page.on('pageerror',error=>unexpected.push(error.message));
  return {context,page,unexpected};
}

async function save(page,name,lang,options={}) {
  await page.evaluate(()=>document.fonts.ready);
  await page.evaluate(()=>scrollTo(0,0));
  await page.screenshot({path:path.join(output,`${name}.${lang}.png`),animations:'disabled',...options});
  screenshotNames.push(`${name}.${lang}.png`);
}
async function cropSection(page,name,lang,first,last) {
  const bounds=await page.evaluate(({first,last})=>{
    const start=document.querySelector(first).getBoundingClientRect();
    const end=document.querySelector(last).getBoundingClientRect();
    const container=document.querySelector('.readOnlyContent').getBoundingClientRect();
    return {x:container.x+18,y:start.top+scrollY-14,width:container.width-36,height:end.bottom-start.top+34};
  },{first,last});
  await save(page,name,lang,{fullPage:true,clip:bounds});
}

(async()=>{
  fs.mkdirSync(output,{recursive:true});
  const browser=await chromium.launch({headless:true,...(process.env.CHROME_BIN?{executablePath:process.env.CHROME_BIN}:{})});
  try {
    for(const lang of ['en','fr']) {
      if(only && !only.endsWith('.'+lang))continue;
      let f=await fixture(browser,lang);
      if(!only || only.startsWith('admin-')) {
      await f.page.goto(origin+'/preview/admin');
      await f.page.waitForFunction(()=>window.demoReady && document.querySelectorAll('#subscriberRows tr').length===3);
      await f.page.locator('#HistoryFrom').fill('2026/10/01');
      await f.page.locator('#HistoryTo').fill('2026/10/11');
      await cropSection(f.page,'admin-settings',lang,'h1','#DeliveryTime + .fieldDescription');
      await cropSection(f.page,'admin-subscribers',lang,'h2[data-digest-text="Subscriptions"]','#subscribersStatus');
      await cropSection(f.page,'admin-reports',lang,'h2[data-digest-text="GenerateReports"]','#reportsStatus');
      assert.deepEqual(f.unexpected,[]);await f.context.close();
      } else await f.context.close();

      for(const kind of ['login','browse','search','archive','subscription']) {
        if(only && only!=='user-'+kind+'.'+lang)continue;
        const mobile=['login','subscription'].includes(kind);
        f=await fixture(browser,lang,mobile);
        if(kind!=='login')await f.context.addInitScript(()=>{
          if(window.top !== window)return;
          localStorage.setItem('library-hub:/emby:token','fictional-demo-token');
          localStorage.setItem('library-hub:/emby:browseLibraries','["10"]');
        });
        const route={login:'Browse',browse:'Browse',search:'Search',archive:'Archive/2026-10-11',subscription:'Subscriptions'}[kind];
        await f.page.goto(kind==='archive'?origin+'/preview/report':origin+prefix+'/'+route+'?lang='+lang);
        if(kind==='login')await f.page.locator('#loginPanel').waitFor({state:'visible'});
        if(kind==='subscription')await f.page.locator('#membershipRows button').waitFor({state:'visible'});
        if(kind==='archive') {
          await f.page.locator('.library').first().waitFor();
        }
        if(kind==='browse'||kind==='search'){
          await f.page.locator('.library-choice').first().waitFor();
          if(kind==='search'){
            await f.page.locator('#browseSearchField').selectOption('Actor');
            await f.page.locator('#browseKind').selectOption('Movie');
            await f.page.locator('#browsePlayed').selectOption('false');
            await f.page.locator('#browseSearch').fill('Gary Oldman');
          }
          const summary=f.page.locator('details[data-level="group"] > summary').first();
          await summary.click();
          await f.page.locator('.browse-item').first().waitFor();
          if(kind==='search')await f.page.setViewportSize({width:1120,height:1320});
        }
        assert(await f.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Horizontal overflow in '+kind+' '+lang);
        await save(f.page,'user-'+kind,lang,{fullPage:mobile});
        assert.deepEqual(f.unexpected,[]);await f.context.close();
      }
    }
    console.log('Captured '+screenshotNames.length+' screenshots; no external requests, writes, JS errors, or horizontal overflow.');
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
