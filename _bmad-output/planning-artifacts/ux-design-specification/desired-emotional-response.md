# Desired Emotional Response

## Primary Emotional Goals

**Erik (Recruiting Leader): Relief and control**
The dominant emotion is relief -- "I don't have to chase people for updates anymore." Erik should feel in control of the recruitment without being the bottleneck. He sees the pipeline, trusts that outcomes are being recorded by his team, and focuses his energy on decisions (who to interview, who to offer) rather than information relay. The app removes the anxiety of "does everyone have the right information?"

**Lina (SME/Collaborator): Efficiency and professionalism**
Lina should feel both "that was quick" AND "my work is properly documented." These aren't competing emotions -- the screening flow should make her feel like a professional doing thorough work efficiently, not like she's rushing through a checkbox exercise. When she closes the app after a screening session, the feeling is: "I'm done. No follow-up emails, no 'can you send me your notes.' It's all in there."

**Anders (Viewer): Confidence and independence**
Anders should feel informed and self-sufficient. He opens the overview and immediately knows where the recruitment stands -- no anxiety about being out of the loop, no dependence on Erik scheduling an update meeting. The emotion is quiet confidence: "I know what's happening."

**The team as a whole: Shared momentum**
Beyond individual emotions, the app should create a collective feeling of progress. When Lina finishes screening and Erik opens the overview and sees "Lina screened 47 candidates today," the team emotion is "we're making progress together without having to coordinate." Showing who recorded outcomes (not just that they were recorded) reinforces this shared momentum and makes contributions visible.

## Emotional Journey Mapping

| Stage | Erik | Lina | Anders |
|-------|------|------|--------|
| **First open** | Curiosity → "this looks manageable" | Neutral → "ok, what do I need to do?" | Interest → "I can see everything already?" |
| **First import** | Mild anxiety → relief ("it matched the CVs correctly") | N/A | N/A |
| **Screening session** | Trust ("Lina's working through them") | Flow → accomplishment ("done, all documented") | N/A |
| **Status check** | Relief ("I can see where we stand without asking anyone") | Contribution ("47 passed screening -- my work moved the pipeline") | Confidence ("I know exactly where this stands") |
| **Error/problem** | "The system caught this and is showing me how to fix it" | "I understand what went wrong, it's not blocking me" | N/A (viewer role, no error-prone actions) |
| **Returning** | Comfort → habituation ("checking this is as natural as checking email") | Minimal friction ("pick up where I left off") | Familiarity ("same view, updated numbers") |

**Long-term emotional target: Habituation.** After 2-3 weeks of use, checking the overview should feel as routine as checking email. Not exciting, not delightful -- just an invisible part of running the recruitment. This is the highest compliment for a B2B tool: it becomes infrastructure.

## Micro-Emotions

**Critical to get right:**
- **Confidence over confusion** -- Every screen must immediately communicate "you're in the right place, here's what you can do." No ambiguity about what action to take next. This includes an undo affordance during the outcome confirmation transition -- Lina must feel "I can't make an irreversible mistake by accident," especially during her first screening session.
- **Trust over skepticism** -- When Erik sees candidate counts on the overview, he needs to trust they're accurate and current. This is a data consistency requirement: if the overview shows "23 at Screening," the candidate list filtered to Screening must show exactly 23. Any divergence, even by one, destroys trust permanently.
- **Accomplishment over frustration** -- After Lina records an outcome, the brief confirmation transition reinforces "that worked, it's saved." Screening progress shows both total progress ("47 of 130 screened") and session progress ("12 this session") -- total progress shows the mountain, session progress shows her stride.

**Important but secondary:**
- **Attentive calm over anxiety** -- The import flow will surface issues. Error treatment must distinguish between blocking errors (invalid file format -- stops the process, prominent treatment) and non-blocking issues (3 unmatched CVs -- needs attention but not a crisis). Non-blocking issues use amber/warm tones with an icon: "3 CVs need manual matching." Blocking errors use standard error treatment with clear "what happened and what to do" messaging. Neither uses red alert styling.
- **Competence over overwhelm** -- The overview shows dense information. Visual hierarchy must make it scannable, not intimidating. Anders should feel "I can read this" not "there's too much here."

## Design Implications

| Emotional Goal | UX Design Approach |
|---|---|
| **Relief** (Erik) | Overview loads with clear counts and status indicators. No actions required to see the full picture. Information comes to him. |
| **Efficiency** (Lina) | Screening flow auto-advances, keyboard shortcuts work, reason field is inline. Minimal clicks per candidate. Session feels like flow, not friction. |
| **Confidence** (Anders) | Data is presented with clear labels, timestamps ("last updated"), and unambiguous visual indicators. No tooltip-dependent information. |
| **Shared momentum** (team) | Outcomes show who recorded them. Overview reflects team activity, not just aggregate counts. Contributions are visible. |
| **Trust** (all) | Real-time data, no stale caches. Overview counts must exactly match filtered list counts -- data consistency is non-negotiable. Outcome confirmation transitions with brief undo affordance. |
| **Attentive calm during errors** | Import distinguishes blocking errors (prominent, stops process) from non-blocking issues (amber tones, icon, specific count, clear resolution path). Neither uses red alert styling. |
| **Accomplishment** (Lina) | Dual progress indicators: total screening progress ("47 of 130") and session progress ("12 this session"). Brief visual confirmation on outcome recording with undo window. |
| **Habituation** (Erik, long-term) | Consistent layout, predictable data positions, fast loading. The overview becomes a daily glance, not an event. Design for routine, not novelty. |

## Emotional Design Principles

1. **The tool earns the right to exist** -- This isn't a tool people asked for. Erik is building it to solve his problem, and Lina and Anders are being invited in. Every interaction must prove the tool is worth using instead of falling back to Teams. Every single session should leave the user thinking "that was better than how I was doing it before." This is the emotional bar -- not delight, not surprise, but undeniable utility.

2. **Information is reassurance** -- Every piece of data visible on the overview is one less question Erik needs to ask, one less meeting Anders needs to attend. Dense information done right creates calm, not overwhelm. The emotional design of the overview is: "everything you need is here, nothing is hidden."

3. **Errors are guidance, not alarms** -- When something goes wrong, the system's tone is helpful and specific. Blocking errors are prominent but clear ("can't parse this file -- expected .xlsx format"). Non-blocking issues use attentive calm -- amber tones, specific counts, clear next steps ("3 CVs need manual matching"). The user should feel "I can handle this" not "something broke."

4. **Completion is visible, mistakes are reversible** -- Users need to see that their actions had effect. Outcome confirmation transitions, screening progress counts, updated overview numbers after a session. The micro-feeling of "that worked" repeated 130 times builds deep trust. And if a mistake happens, the undo affordance during the confirmation transition means "I can't accidentally do something I can't fix."

5. **Design for habituation** -- The long-term emotional target is not excitement but invisibility. After a few weeks, checking the overview should feel like checking email. Consistent layouts, predictable data positions, fast loading. The tool becomes infrastructure -- the highest compliment for a B2B internal tool.
