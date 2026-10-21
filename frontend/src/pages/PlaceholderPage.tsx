/** Empty state for pages that are planned but not built yet. */
export default function PlaceholderPage({ title }: { title: string }) {
  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      <div className="mt-6 rounded-lg border border-dashed border-slate-300 bg-white p-10 text-center">
        <p className="font-medium text-slate-700">Not built yet</p>
        <p className="mt-1 text-sm text-slate-500">This page is part of an upcoming build step.</p>
      </div>
    </div>
  )
}
